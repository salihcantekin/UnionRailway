using Microsoft.AspNetCore.Http;
using UnionRailway;

namespace UnionRailway.AspNetCore;

/// <summary>
/// Provides extension methods that convert <see cref="Result{T}"/> into
/// ASP.NET Core <see cref="IResult"/> responses for Minimal APIs and Controllers.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="IResult"/>.
    /// <list type="bullet">
    ///   <item><see cref="Result{T}.Ok"/> maps to <c>Results.Ok(data)</c>.</item>
    ///   <item><see cref="UnionError.NotFound"/> maps to <c>Results.NotFound()</c>.</item>
    ///   <item><see cref="UnionError.Conflict"/> maps to <c>Results.Conflict()</c>.</item>
    ///   <item><see cref="UnionError.Unauthorized"/> maps to <c>Results.Unauthorized()</c>.</item>
    ///   <item><see cref="UnionError.Validation"/> maps to <c>Results.BadRequest()</c>.</item>
    ///   <item><see cref="UnionError.SystemFailure"/> maps to <c>Results.Problem()</c> (500).</item>
    /// </list>
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An <see cref="IResult"/> representing the appropriate HTTP response.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result switch
        {
            Result<T>.Ok(var data) => Results.Ok(data),
            Result<T>.Error(var err) => err.ToHttpResult(),
            _ => Results.StatusCode(500)
        };

    /// <summary>
    /// Converts a <see cref="UnionError"/> to an <see cref="IResult"/>.
    /// </summary>
    /// <param name="error">The error to convert.</param>
    /// <returns>An <see cref="IResult"/> representing the appropriate HTTP error response.</returns>
    public static IResult ToHttpResult(this UnionError error) =>
        error switch
        {
            UnionError.NotFound(var resource) => Results.NotFound(new { Resource = resource }),
            UnionError.Conflict(var reason) => Results.Conflict(new { Reason = reason }),
            UnionError.Unauthorized => Results.Unauthorized(),
            UnionError.Validation(var fields) => Results.BadRequest(new { Errors = fields }),
            UnionError.SystemFailure(var ex) => Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Internal Server Error"),
            _ => Results.StatusCode(500)
        };
}
