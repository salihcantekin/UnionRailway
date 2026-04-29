namespace UnionRailway;

/// <summary>
/// Developer-experience extension methods for <see cref="Rail{T}"/>.
/// </summary>
public static class UnionExtensions
{
    /// <summary>
    /// Deconstructs the rail for the common early-return pattern.
    /// </summary>
    public static bool IsSuccess<T>(
        this Rail<T> result,
        [MaybeNull] out T data,
        out UnionError? error)
    {
        var isSuccess = result.TryGetValue(out data);
        error = result.TryGetError(out var foundError) ? foundError : default;
        return isSuccess;
    }

    /// <summary>
    /// Returns the success value, or throws <see cref="UnwrapException"/> when the rail carries an error.
    /// </summary>
    public static T Unwrap<T>(this Rail<T> result)
    {
        if (result.TryGetError(out var error))
        {
            throw new UnwrapException(error);
        }

        if (result.TryGetValue(out var value))
        {
            return value;
        }

        throw new UnwrapException(null);
    }

    /// <summary>
    /// Returns the success value when present; otherwise <paramref name="defaultValue"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T UnwrapOrDefault<T>(this Rail<T> result, T defaultValue) =>
        result.TryGetValue(out var value) ? value : defaultValue;

    /// <summary>
    /// Routes the rail to either <paramref name="onOk"/> or <paramref name="onError"/>.
    /// </summary>
    public static TResult Match<T, TResult>(
        this Rail<T> result,
        Func<T, TResult> onOk,
        Func<UnionError, TResult> onError)
    {
        ArgumentNullException.ThrowIfNull(onOk);
        ArgumentNullException.ThrowIfNull(onError);

        if (result.TryGetValue(out var value))
        {
            return onOk(value);
        }

        if (result.TryGetError(out var error))
        {
            return onError(error.GetValueOrDefault());
        }

        throw new UnwrapException(null);
    }

    /// <summary>Transforms the success value and preserves any error.</summary>
    public static Rail<TOut> Map<T, TOut>(this Rail<T> result, Func<T, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (result.TryGetError(out var error))
        {
            return error.GetValueOrDefault();
        }

        if (result.TryGetValue(out var value))
        {
            return mapper(value);
        }

        return Union.Fail<TOut>(new UnionError.SystemFailure(new InvalidOperationException("Rail result was uninitialized.")));
    }

    /// <summary>Transforms the success value asynchronously and preserves any error.</summary>
    public static async ValueTask<Rail<TOut>> MapAsync<T, TOut>(
        this Rail<T> result,
        Func<T, ValueTask<TOut>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (result.TryGetError(out var error))
        {
            return error.GetValueOrDefault();
        }

        if (result.TryGetValue(out var value))
        {
            return await mapper(value);
        }

        return Union.Fail<TOut>(new UnionError.SystemFailure(new InvalidOperationException("Rail result was uninitialized.")));
    }

    /// <summary>Binds the success value to another rail and preserves any error.</summary>
    public static Rail<TOut> Bind<T, TOut>(this Rail<T> result, Func<T, Rail<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetError(out var error))
        {
            return error.GetValueOrDefault();
        }

        if (result.TryGetValue(out var value))
        {
            return binder(value);
        }

        return Union.Fail<TOut>(new UnionError.SystemFailure(new InvalidOperationException("Rail result was uninitialized.")));
    }

    /// <summary>Binds the success value to another rail asynchronously and preserves any error.</summary>
    public static async ValueTask<Rail<TOut>> BindAsync<T, TOut>(
        this Rail<T> result,
        Func<T, ValueTask<Rail<TOut>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetError(out var error))
        {
            return error.GetValueOrDefault();
        }

        if (result.TryGetValue(out var value))
        {
            return await binder(value);
        }

        return Union.Fail<TOut>(new UnionError.SystemFailure(new InvalidOperationException("Rail result was uninitialized.")));
    }

    /// <summary>Executes a side effect for a success value and returns the original rail.</summary>
    public static Rail<T> Tap<T>(this Rail<T> result, Action<T> onOk)
    {
        ArgumentNullException.ThrowIfNull(onOk);

        if (result.TryGetValue(out var value))
        {
            onOk(value);
        }

        return result;
    }

    /// <summary>Executes a side effect for a success value asynchronously and returns the original rail.</summary>
    public static async ValueTask<Rail<T>> TapAsync<T>(this Rail<T> result, Func<T, ValueTask> onOk)
    {
        ArgumentNullException.ThrowIfNull(onOk);

        if (result.TryGetValue(out var value))
        {
            await onOk(value);
        }

        return result;
    }

    /// <summary>
    /// Recovers from a specific error type by providing a fallback value.
    /// If the rail contains an error of type <typeparamref name="TError"/>,
    /// the <paramref name="recovery"/> function is invoked and its result
    /// replaces the error. Other error types pass through unchanged.
    /// </summary>
    public static Rail<T> Recover<T, TError>(
        this Rail<T> result,
        Func<TError, T> recovery)
        where TError : class
    {
        ArgumentNullException.ThrowIfNull(recovery);

        if (result.TryGetError(out var error) && error.GetValueOrDefault().Value is TError typed)
        {
            return recovery(typed);
        }

        return result;
    }

    /// <summary>
    /// Recovers from a specific error type by providing an asynchronous fallback value.
    /// If the rail contains an error of type <typeparamref name="TError"/>,
    /// the <paramref name="recovery"/> function is invoked and its result
    /// replaces the error. Other error types pass through unchanged.
    /// </summary>
    public static async ValueTask<Rail<T>> RecoverAsync<T, TError>(
        this Rail<T> result,
        Func<TError, ValueTask<T>> recovery)
        where TError : class
    {
        ArgumentNullException.ThrowIfNull(recovery);

        if (result.TryGetError(out var error) && error.GetValueOrDefault().Value is TError typed)
        {
            return await recovery(typed);
        }

        return result;
    }
}
