using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnionRailway.AspNetCore;

/// <summary>
/// Extension methods for translating <see cref="Rail{T}"/> values and
/// <see cref="UnionError"/> values into ASP.NET Core <see cref="IResult"/>
/// responses with full RFC 7807 <see cref="ProblemDetails"/> mapping.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Converts an asynchronous rail to an <see cref="IResult"/>.
    /// </summary>
    public static async Task<IResult> ToHttpResultAsync<T>(
        this Task<Rail<T>> resultTask,
        string? createdUri = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        return (await resultTask).ToHttpResult(createdUri);
    }

    /// <summary>
    /// Converts an asynchronous rail to an <see cref="IResult"/>.
    /// </summary>
    public static async ValueTask<IResult> ToHttpResultAsync<T>(
        this ValueTask<Rail<T>> resultTask,
        string? createdUri = null) =>
        (await resultTask).ToHttpResult(createdUri);

    /// <summary>
    /// Converts a rail to an <see cref="IResult"/>:
    /// <list type="bullet">
    ///   <item>Success → <c>200 OK</c>, or <c>201 Created</c> when <paramref name="createdUri"/> is supplied.</item>
    ///   <item>Error → delegates to <see cref="ToHttpResult(UnionError)"/>.</item>
    /// </list>
    /// </summary>
    public static IResult ToHttpResult<T>(
        this Rail<T> result,
        string? createdUri = null)
    {
        if (result.TryGetError(out var error))
        {
            return error.GetValueOrDefault().ToHttpResult();
        }

        T? value = result.Unwrap();
        return createdUri is not null
            ? Results.Created(createdUri, value)
            : Results.Ok(value);
    }

    /// <summary>
    /// Translates a <see cref="UnionError"/> into the appropriate RFC 7807
    /// <see cref="ProblemDetails"/> response:
    /// <list type="bullet">
    ///   <item><see cref="UnionError.NotFound"/>      → 404 Not Found</item>
    ///   <item><see cref="UnionError.Conflict"/>      → 409 Conflict</item>
    ///   <item><see cref="UnionError.Unauthorized"/>  → 401 Unauthorized</item>
    ///   <item><see cref="UnionError.Forbidden"/>     → 403 Forbidden</item>
    ///   <item><see cref="UnionError.Validation"/>    → 400 Bad Request (ValidationProblemDetails)</item>
    ///   <item><see cref="UnionError.SystemFailure"/> → 500 Internal Server Error</item>
    /// </list>
    /// </summary>
    public static IResult ToHttpResult(this UnionError error) => error.Value switch
    {
        UnionError.NotFound nf =>
            Results.Problem(
                detail: $"The resource '{nf.Resource}' was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"),

        UnionError.Conflict c =>
            Results.Problem(
                detail: c.Reason,
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

        UnionError.Unauthorized =>
            Results.Problem(
                detail: "Authentication is required to access this resource.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                type: "https://tools.ietf.org/html/rfc7235#section-3.1"),

        UnionError.Forbidden f =>
            Results.Problem(
                detail: f.Reason,
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.3"),

        UnionError.Validation v =>
            BuildValidationProblem(v),

        UnionError.SystemFailure =>
            Results.Problem(
                detail: "An unexpected error occurred. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1"),

        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    private static IResult BuildValidationProblem(UnionError.Validation v)
    {
        var errors = new Dictionary<string, string[]>(
            v.Fields.Select(kv => KeyValuePair.Create(kv.Key, kv.Value)),
            StringComparer.Ordinal);

        return Results.ValidationProblem(
            errors: errors,
            title: "One or more validation errors occurred.",
            type: "https://tools.ietf.org/html/rfc4918#section-11.2");
    }
}
