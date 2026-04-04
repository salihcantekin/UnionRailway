namespace UnionRailway;

/// <summary>
/// Provides pragmatic extension methods for <see cref="Result{T}"/>
/// that enable simple early-return patterns with <c>out</c> parameters.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Checks whether the result represents a success and deconstructs both branches
    /// via <c>out</c> parameters. Designed for early-return control flow.
    /// </summary>
    /// <example>
    /// <code>
    /// if (!result.IsSuccess(out var user, out var error))
    ///     return error;
    /// // use user safely here
    /// </code>
    /// </example>
    /// <param name="result">The result to check.</param>
    /// <param name="data">When successful, contains the data; otherwise <c>default</c>.</param>
    /// <param name="error">When failed, contains the error; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if the result is <see cref="Result{T}.Ok"/>; otherwise <c>false</c>.</returns>
    public static bool IsSuccess<T>(this Result<T> result, out T? data, out UnionError? error)
    {
        switch (result)
        {
            case Result<T>.Ok(var value):
                data = value;
                error = null;
                return true;

            case Result<T>.Error(var err):
                data = default;
                error = err;
                return false;

            default:
                data = default;
                error = null;
                return false;
        }
    }

    /// <summary>
    /// Asynchronous version of <see cref="IsSuccess{T}"/>. Awaits the task and then
    /// deconstructs the result.
    /// </summary>
    /// <param name="resultTask">The asynchronous result to check.</param>
    /// <returns>
    /// A tuple containing: whether the result is a success, the data (if success), and the error (if failure).
    /// </returns>
    public static async Task<(bool Success, T? Data, UnionError? Error)> IsSuccessAsync<T>(
        this Task<Result<T>> resultTask)
    {
        var result = await resultTask.ConfigureAwait(false);

        return result switch
        {
            Result<T>.Ok(var value) => (true, value, null),
            Result<T>.Error(var err) => (false, default, err),
            _ => (false, default, null)
        };
    }

    /// <summary>
    /// Converts a nullable reference to a <see cref="Result{T}"/>.
    /// Returns <see cref="Result{T}.Ok"/> if the value is not null,
    /// or <see cref="Result{T}.Error"/> with a <see cref="UnionError.NotFound"/> if it is null.
    /// </summary>
    /// <param name="value">The nullable value to convert.</param>
    /// <param name="resourceName">A description of the resource for the error message.</param>
    public static Result<T> ToResult<T>(this T? value, string resourceName) where T : class =>
        value is not null
            ? new Result<T>.Ok(value)
            : new Result<T>.Error(new UnionError.NotFound(resourceName));

    /// <summary>
    /// Converts a nullable value type to a <see cref="Result{T}"/>.
    /// Returns <see cref="Result{T}.Ok"/> if the value has a value,
    /// or <see cref="Result{T}.Error"/> with a <see cref="UnionError.NotFound"/> if it does not.
    /// </summary>
    /// <param name="value">The nullable value to convert.</param>
    /// <param name="resourceName">A description of the resource for the error message.</param>
    public static Result<T> ToResult<T>(this T? value, string resourceName) where T : struct =>
        value.HasValue
            ? new Result<T>.Ok(value.Value)
            : new Result<T>.Error(new UnionError.NotFound(resourceName));
}
