using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnionRailway.AspNetCore;

/// <summary>
/// Extension methods for translating <see cref="Rail{T}"/> values and <see cref="UnionError"/> values into ASP.NET Core
/// <see cref="IResult"/> responses with full RFC 7807 <see cref="ProblemDetails"/> mapping.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Converts an asynchronous rail to an <see cref="IResult"/>. When a <paramref name="cancellationToken"/> is
    /// provided, cancellation is checked before awaiting the result task.
    /// </summary>
    public static async Task<IResult> ToHttpResultAsync<T>(
        this Task<Rail<T>> resultTask,
        string? createdUri = null,
        Action<ProblemDetails>? configureProblem = null,
        CancellationToken cancellationToken = default,
        IUnionErrorMapper? errorMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        cancellationToken.ThrowIfCancellationRequested();
        return (await resultTask).ToHttpResult(createdUri, configureProblem, errorMapper);
    }

    /// <summary>
    /// Converts an asynchronous rail to an <see cref="IResult"/>. When a <paramref name="cancellationToken"/> is
    /// provided, cancellation is checked before awaiting the result task.
    /// </summary>
    public static async ValueTask<IResult> ToHttpResultAsync<T>(
        this ValueTask<Rail<T>> resultTask,
        string? createdUri = null,
        Action<ProblemDetails>? configureProblem = null,
        CancellationToken cancellationToken = default,
        IUnionErrorMapper? errorMapper = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (await resultTask).ToHttpResult(createdUri, configureProblem, errorMapper);
    }

    /// <summary>
    /// Converts a rail to an <see cref="IResult"/>: <list type="bullet"> <item> Success with <see cref="Unit"/> → <c>
    /// 204 No Content</c>.</item> <item> Success → <c> 200 OK</c>, or <c> 201 Created</c> when
    /// <paramref name="createdUri"/> is supplied.</item> <item> Error → delegates to
    /// <see cref="ToHttpResult(UnionError, Action{ProblemDetails}?)"/> .</item> </list>
    /// </summary>
    public static IResult ToHttpResult<T>(
        this Rail<T> result,
        string? createdUri = null,
        Action<ProblemDetails>? configureProblem = null,
        IUnionErrorMapper? errorMapper = null)
    {
        if (result.TryGetError(out var error))
        {
            return error.GetValueOrDefault().ToHttpResult(configureProblem, errorMapper);
        }

        T? value = result.Unwrap();

        if (value is Unit)
        {
            return Results.NoContent();
        }

        return createdUri is not null
            ? Results.Created(createdUri, value)
            : Results.Ok(value);
    }

    /// <summary>
    /// Translates a <see cref="UnionError"/> into the appropriate RFC 7807 <see cref="ProblemDetails"/> response:
    /// <list type="bullet"> <item> <see cref="UnionError.NotFound"/> → 404 Not Found</item> <item>
    /// <see cref="UnionError.Conflict"/> → 409 Conflict</item> <item> <see cref="UnionError.Unauthorized"/> → 401
    /// Unauthorized</item> <item> <see cref="UnionError.Forbidden"/> → 403 Forbidden</item> <item>
    /// <see cref="UnionError.Validation"/> → 400 Bad Request (ValidationProblemDetails)</item> <item>
    /// <see cref="UnionError.SystemFailure"/> → 500 Internal Server Error</item> <item> <see cref="UnionError.Custom"/>
    /// → Custom status code (default 422)</item> </list>
    /// </summary>
    /// <param name="error">The error to translate.</param>
    /// <param name="configureProblem">
    /// Optional callback to post-process the <see cref="ProblemDetails"/> before it is returned. Use this to add trace
    /// IDs, strip detail in production, or enrich extensions.
    /// </param>
    public static IResult ToHttpResult(
        this UnionError error,
        Action<ProblemDetails>? configureProblem = null,
        IUnionErrorMapper? errorMapper = null)
    {
        if (errorMapper?.TryMap(error) is { } customResult)
        {
            return customResult;
        }

        return error.Value switch
        {
            UnionError.NotFound nf =>
                BuildProblem(
                    detail: $"The resource '{nf.Resource}' was not found.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    configureProblem: configureProblem),

            UnionError.Conflict c =>
                BuildProblem(
                    detail: c.Reason,
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict",
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    configureProblem: configureProblem),

            UnionError.Unauthorized =>
                BuildProblem(
                    detail: "Authentication is required to access this resource.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    type: "https://tools.ietf.org/html/rfc7235#section-3.1",
                    configureProblem: configureProblem),

            UnionError.Forbidden f =>
                BuildProblem(
                    detail: f.Reason,
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    configureProblem: configureProblem),

            UnionError.Validation v =>
                BuildValidationProblem(v, configureProblem),

            UnionError.SystemFailure =>
                BuildProblem(
                    detail: "An unexpected error occurred. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error",
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    configureProblem: configureProblem),

            UnionError.Custom c =>
                BuildCustomProblem(c, configureProblem),

            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static IResult BuildProblem(
        string? detail,
        int statusCode,
        string title,
        string type,
        Action<ProblemDetails>? configureProblem)
    {
        if (configureProblem is null)
        {
            return Results.Problem(
                detail: detail,
                statusCode: statusCode,
                title: title,
                type: type);
        }

        var pd = new ProblemDetails
        {
            Detail = detail,
            Status = statusCode,
            Title = title,
            Type = type
        };

        configureProblem(pd);
        return Results.Problem(pd);
    }

    private static IResult BuildValidationProblem(
        UnionError.Validation v,
        Action<ProblemDetails>? configureProblem)
    {
        var errors = new Dictionary<string, string[]>(
            v.Fields.Select(kv => KeyValuePair.Create(kv.Key, kv.Value)),
            StringComparer.Ordinal);

        if (configureProblem is null)
        {
            return Results.ValidationProblem(
                errors: errors,
                title: "One or more validation errors occurred.",
                type: "https://tools.ietf.org/html/rfc4918#section-11.2");
        }

        var pd = new HttpValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
            Status = StatusCodes.Status400BadRequest
        };

        configureProblem(pd);
        return Results.Problem(pd);
    }

    private static IResult BuildCustomProblem(
        UnionError.Custom c,
        Action<ProblemDetails>? configureProblem)
    {
        var pd = new ProblemDetails
        {
            Detail = c.Message,
            Status = c.StatusCode,
            Title = c.Code,
            Type = "https://tools.ietf.org/html/rfc7231#section-6"
        };

        pd.Extensions["errorCode"] = c.Code;

        if (c.Extensions is not null)
        {
            foreach (var kvp in c.Extensions)
            {
                pd.Extensions[kvp.Key] = kvp.Value;
            }
        }

        configureProblem?.Invoke(pd);
        return Results.Problem(pd);
    }
}
