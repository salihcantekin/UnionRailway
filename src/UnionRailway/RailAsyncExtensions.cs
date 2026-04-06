namespace UnionRailway;

/// <summary>
/// Async composition helpers for <see cref="Task{TResult}"/> and <see cref="ValueTask{TResult}"/>
/// carrying <see cref="Rail{T}"/> values.
/// </summary>
public static class RailAsyncExtensions
{
    public static async Task<T> UnwrapAsync<T>(this Task<Rail<T>> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        return (await resultTask).Unwrap();
    }

    public static async ValueTask<T> UnwrapAsync<T>(this ValueTask<Rail<T>> resultTask) =>
        (await resultTask).Unwrap();

    public static async Task<T> UnwrapOrDefaultAsync<T>(this Task<Rail<T>> resultTask, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        return (await resultTask).UnwrapOrDefault(defaultValue);
    }

    public static async ValueTask<T> UnwrapOrDefaultAsync<T>(this ValueTask<Rail<T>> resultTask, T defaultValue) =>
        (await resultTask).UnwrapOrDefault(defaultValue);

    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<Rail<T>> resultTask,
        Func<T, TResult> onOk,
        Func<UnionError, TResult> onError)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        return (await resultTask).Match(onOk, onError);
    }

    public static async ValueTask<TResult> MatchAsync<T, TResult>(
        this ValueTask<Rail<T>> resultTask,
        Func<T, TResult> onOk,
        Func<UnionError, TResult> onError) =>
        (await resultTask).Match(onOk, onError);

    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<Rail<T>> resultTask,
        Func<T, Task<TResult>> onOk,
        Func<UnionError, Task<TResult>> onError)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(onOk);
        ArgumentNullException.ThrowIfNull(onError);

        var result = await resultTask;
        if (result.TryGetValue(out var value))
            return await onOk(value);

        if (result.TryGetError(out var error))
            return await onError(error.GetValueOrDefault());

        throw new UnwrapException(null);
    }

    public static async ValueTask<TResult> MatchAsync<T, TResult>(
        this ValueTask<Rail<T>> resultTask,
        Func<T, ValueTask<TResult>> onOk,
        Func<UnionError, ValueTask<TResult>> onError)
    {
        ArgumentNullException.ThrowIfNull(onOk);
        ArgumentNullException.ThrowIfNull(onError);

        var result = await resultTask;
        if (result.TryGetValue(out var value))
            return await onOk(value);

        if (result.TryGetError(out var error))
            return await onError(error.GetValueOrDefault());

        throw new UnwrapException(null);
    }

    public static async Task<Rail<TOut>> MapAsync<T, TOut>(this Task<Rail<T>> resultTask, Func<T, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        return (await resultTask).Map(mapper);
    }

    public static async Task<Rail<TOut>> MapAsync<T, TOut>(this Task<Rail<T>> resultTask, Func<T, Task<TOut>> mapper)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);

        var result = await resultTask;
        if (result.TryGetError(out var error))
            return Union.Fail<TOut>(error.GetValueOrDefault());

        if (result.TryGetValue(out var value))
            return await mapper(value);

        return Union.Fail<TOut>(new UnionError.SystemFailure(new InvalidOperationException("Rail result was uninitialized.")));
    }

    public static async ValueTask<Rail<TOut>> MapAsync<T, TOut>(this ValueTask<Rail<T>> resultTask, Func<T, TOut> mapper) =>
        (await resultTask).Map(mapper);

    public static async ValueTask<Rail<TOut>> MapAsync<T, TOut>(this ValueTask<Rail<T>> resultTask, Func<T, ValueTask<TOut>> mapper) =>
        await (await resultTask).MapAsync(mapper);

    public static async Task<Rail<TOut>> BindAsync<T, TOut>(this Task<Rail<T>> resultTask, Func<T, Rail<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        return (await resultTask).Bind(binder);
    }

    public static async Task<Rail<TOut>> BindAsync<T, TOut>(this Task<Rail<T>> resultTask, Func<T, Task<Rail<TOut>>> binder)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(binder);

        var result = await resultTask;
        if (result.TryGetError(out var error))
            return Union.Fail<TOut>(error.GetValueOrDefault());

        if (result.TryGetValue(out var value))
            return await binder(value);

        return Union.Fail<TOut>(new UnionError.SystemFailure(new InvalidOperationException("Rail result was uninitialized.")));
    }

    public static async ValueTask<Rail<TOut>> BindAsync<T, TOut>(this ValueTask<Rail<T>> resultTask, Func<T, Rail<TOut>> binder) =>
        (await resultTask).Bind(binder);

    public static async ValueTask<Rail<TOut>> BindAsync<T, TOut>(this ValueTask<Rail<T>> resultTask, Func<T, ValueTask<Rail<TOut>>> binder) =>
        await (await resultTask).BindAsync(binder);

    public static async Task<Rail<T>> TapAsync<T>(this Task<Rail<T>> resultTask, Action<T> onOk)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        return (await resultTask).Tap(onOk);
    }

    public static async Task<Rail<T>> TapAsync<T>(this Task<Rail<T>> resultTask, Func<T, Task> onOk)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(onOk);

        var result = await resultTask;
        if (result.TryGetValue(out var value))
            await onOk(value);

        return result;
    }

    public static async ValueTask<Rail<T>> TapAsync<T>(this ValueTask<Rail<T>> resultTask, Action<T> onOk) =>
        (await resultTask).Tap(onOk);

    public static async ValueTask<Rail<T>> TapAsync<T>(this ValueTask<Rail<T>> resultTask, Func<T, ValueTask> onOk) =>
        await (await resultTask).TapAsync(onOk);
}
