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

    /// <summary>
    /// Binds the success value to another rail when the output type is <see cref="object"/>.
    /// This overload prevents generic type inference issues with anonymous types and boxed values.
    /// </summary>
    /// <typeparam name="T">The input success type.</typeparam>
    /// <param name="result">The input rail.</param>
    /// <param name="binder">Function that maps the success value to a new rail containing an object.</param>
    /// <returns>A new rail with the bound result, or the original error if present.</returns>
    /// <remarks>
    /// <para><b>Why use this?</b></para>
    /// <para>
    /// When using <see cref="Bind{T, TOut}"/> with anonymous types or <see cref="object"/> results,
    /// C# generic type inference may incorrectly resolve the output type, causing the Rail wrapper
    /// to be serialized instead of the inner value. <c>BindObject</c> explicitly constrains the 
    /// output type to <see cref="object"/>, preventing these issues.
    /// </para>
    /// <para><b>Example:</b></para>
    /// <code>
    /// var result = await GetProductAsync(id)
    ///     .BindObject(product => product.Stock > 0
    ///         ? Union.Ok(new { product.Id, product.Name, Status = "Available" })
    ///         : Union.Fail&lt;object&gt;(new UnionError.Conflict("Out of stock")));
    /// </code>
    /// </remarks>
    public static Rail<object> BindObject<T>(
        this Rail<T> result,
        Func<T, Rail<object>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetError(out var error))
            return error.GetValueOrDefault();

        if (result.TryGetValue(out var value))
            return binder(value);

        return Union.Fail<object>(new UnionError.SystemFailure(
            new InvalidOperationException("Rail result was uninitialized.")));
    }

    /// <summary>
    /// Binds with inline conditional logic for <see cref="object"/> results.
    /// This is the recommended pattern for conditional error handling when returning anonymous types or boxed values.
    /// </summary>
    /// <typeparam name="T">The input success type.</typeparam>
    /// <param name="result">The input rail.</param>
    /// <param name="predicate">Condition to check on the success value.</param>
    /// <param name="onSuccess">Function to create the success value (boxed as object) when predicate is true.</param>
    /// <param name="onError">Function to create the error when predicate is false.</param>
    /// <returns>A rail containing the success object or an error, depending on the predicate result.</returns>
    /// <remarks>
    /// <para><b>Cleaner Alternative to if/else in Bind</b></para>
    /// <para>
    /// This overload separates the condition, success path, and error path into distinct parameters,
    /// making the code more readable and avoiding nested ternary expressions.
    /// </para>
    /// <para><b>Example:</b></para>
    /// <code>
    /// var result = await GetProductAsync(id)
    ///     .BindObject(
    ///         product => product.Stock > 0,
    ///         product => new { product.Id, product.Name, Status = "In Stock" },
    ///         product => new UnionError.Conflict("Out of stock"));
    /// </code>
    /// <para><b>Equivalent to:</b></para>
    /// <code>
    /// var result = await GetProductAsync(id)
    ///     .BindObject(product => product.Stock > 0
    ///         ? Union.Ok(new { product.Id, product.Name, Status = "In Stock" })
    ///         : Union.Fail&lt;object&gt;(new UnionError.Conflict("Out of stock")));
    /// </code>
    /// </remarks>
    public static Rail<object> BindObject<T>(
        this Rail<T> result,
        Func<T, bool> predicate,
        Func<T, object> onSuccess,
        Func<T, UnionError> onError)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        if (result.TryGetError(out var error))
            return error.GetValueOrDefault();

        if (result.TryGetValue(out var value))
        {
            if (predicate(value))
                return onSuccess(value);
            return onError(value);
        }

        return Union.Fail<object>(new UnionError.SystemFailure(
            new InvalidOperationException("Rail result was uninitialized.")));
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
