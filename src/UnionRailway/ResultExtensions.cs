namespace UnionRailway;

/// <summary>
/// Provides functional extension methods for composing and transforming <see cref="Result{T}"/> values
/// in a Railway-Oriented Programming style.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Transforms the success value of a result using the specified mapping function.
    /// If the result is an error, the error is propagated unchanged.
    /// </summary>
    public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> map) =>
        result switch
        {
            Result<T>.Ok(var data) => new Result<TOut>.Ok(map(data)),
            Result<T>.Error(var err) => new Result<TOut>.Error(err),
            _ => throw new InvalidOperationException("Exhaustive match failed on Result<T>.")
        };

    /// <summary>
    /// Chains a result-producing function onto a successful result (monadic bind / flatMap).
    /// If the result is an error, the error is propagated unchanged.
    /// </summary>
    public static Result<TOut> Bind<T, TOut>(this Result<T> result, Func<T, Result<TOut>> bind) =>
        result switch
        {
            Result<T>.Ok(var data) => bind(data),
            Result<T>.Error(var err) => new Result<TOut>.Error(err),
            _ => throw new InvalidOperationException("Exhaustive match failed on Result<T>.")
        };

    /// <summary>
    /// Applies one of two functions depending on whether the result is a success or an error.
    /// </summary>
    public static TOut Match<T, TOut>(this Result<T> result, Func<T, TOut> onOk, Func<UnionError, TOut> onError) =>
        result switch
        {
            Result<T>.Ok(var data) => onOk(data),
            Result<T>.Error(var err) => onError(err),
            _ => throw new InvalidOperationException("Exhaustive match failed on Result<T>.")
        };

    /// <summary>
    /// Executes a side-effect action on the success value without altering the result.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result is Result<T>.Ok(var data))
        {
            action(data);
        }

        return result;
    }

    /// <summary>
    /// Transforms the error of a result using the specified mapping function.
    /// If the result is a success, it is propagated unchanged.
    /// </summary>
    public static Result<T> MapError<T>(this Result<T> result, Func<UnionError, UnionError> map) =>
        result switch
        {
            Result<T>.Ok => result,
            Result<T>.Error(var err) => new Result<T>.Error(map(err)),
            _ => throw new InvalidOperationException("Exhaustive match failed on Result<T>.")
        };

    /// <summary>
    /// Converts a nullable value to a <see cref="Result{T}"/>.
    /// Returns <see cref="Result{T}.Ok"/> if the value is not null,
    /// or <see cref="Result{T}.Error"/> with a <see cref="UnionError.NotFound"/> if it is null.
    /// </summary>
    public static Result<T> ToResult<T>(this T? value, string resourceName) where T : class =>
        value is not null
            ? new Result<T>.Ok(value)
            : new Result<T>.Error(new UnionError.NotFound(resourceName));

    /// <summary>
    /// Converts a nullable value type to a <see cref="Result{T}"/>.
    /// Returns <see cref="Result{T}.Ok"/> if the value has a value,
    /// or <see cref="Result{T}.Error"/> with a <see cref="UnionError.NotFound"/> if it does not.
    /// </summary>
    public static Result<T> ToResult<T>(this T? value, string resourceName) where T : struct =>
        value.HasValue
            ? new Result<T>.Ok(value.Value)
            : new Result<T>.Error(new UnionError.NotFound(resourceName));

    /// <summary>
    /// Asynchronous version of <see cref="Map{T,TOut}"/>.
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, TOut> map) =>
        (await resultTask.ConfigureAwait(false)).Map(map);

    /// <summary>
    /// Asynchronous version of <see cref="Bind{T,TOut}"/>.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Result<TOut>> bind) =>
        (await resultTask.ConfigureAwait(false)).Bind(bind);

    /// <summary>
    /// Asynchronous version of <see cref="Bind{T,TOut}"/> accepting an async binding function.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<TOut>>> bind)
    {
        var result = await resultTask.ConfigureAwait(false);

        return result switch
        {
            Result<T>.Ok(var data) => await bind(data).ConfigureAwait(false),
            Result<T>.Error(var err) => new Result<TOut>.Error(err),
            _ => throw new InvalidOperationException("Exhaustive match failed on Result<T>.")
        };
    }

    /// <summary>
    /// Asynchronous version of <see cref="Match{T,TOut}"/>.
    /// </summary>
    public static async Task<TOut> MatchAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, TOut> onOk,
        Func<UnionError, TOut> onError) =>
        (await resultTask.ConfigureAwait(false)).Match(onOk, onError);

    /// <summary>
    /// Asynchronous version of <see cref="Tap{T}"/>.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Action<T> action) =>
        (await resultTask.ConfigureAwait(false)).Tap(action);
}
