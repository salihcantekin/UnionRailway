namespace UnionRailway;

/// <summary>
/// Zero-allocation developer-experience extension methods for the anonymous
/// union type <c>(T Value, UnionError? Error)</c>.
/// </summary>
/// <remarks>
/// Because <c>(T Value, UnionError? Error)</c> is a <see cref="System.ValueTuple{T1,T2}"/>
/// (a struct), all these methods operate purely on stack memory — no heap
/// allocations occur in any of the happy-path or error-path operations.
/// </remarks>
public static class UnionExtensions
{
    /// <summary>
    /// Deconstructs the union for the common early-return pattern.
    /// Assigns both <paramref name="data"/> and <paramref name="error"/>
    /// so every code path has safe, direct access.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The union to inspect.</param>
    /// <param name="data">
    /// Set to <see cref="ValueTuple{T1,T2}.Item1"/> when <c>true</c> is returned;
    /// otherwise <see langword="default"/>.
    /// </param>
    /// <param name="error">
    /// Set to the <see cref="UnionError"/> when <c>false</c> is returned;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> when the union carries a success value.</returns>
    /// <example>
    /// <code>
    /// if (!result.IsSuccess(out var user, out var err))
    ///     return err!.ToHttpResult();
    /// Console.WriteLine(user.Name); // safe — IsSuccess returned true
    /// </code>
    /// </example>
    public static bool IsSuccess<T>(
        this (T Value, UnionError? Error) result,
        [MaybeNull] out T data,
        out UnionError? error)
    {
        data  = result.Value;
        error = result.Error;

        return result.Error is null;
    }

    /// <summary>
    /// Returns the success value, or throws <see cref="UnwrapException"/>
    /// when the union carries an error.
    /// </summary>
    /// <exception cref="UnwrapException">
    /// Thrown when <c>result.Error</c> is not <see langword="null"/>.
    /// </exception>
    public static T Unwrap<T>(this (T Value, UnionError? Error) result)
    {
        if (result.Error is not null)
            throw new UnwrapException(result.Error);

        return result.Value;
    }

    /// <summary>
    /// Returns the success value when present; otherwise <paramref name="defaultValue"/>.
    /// Never throws.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T UnwrapOrDefault<T>(
        this (T Value, UnionError? Error) result,
        T defaultValue)
        => result.Error is null ? result.Value : defaultValue;

    /// <summary>
    /// Routes the union to either <paramref name="onOk"/> or <paramref name="onError"/>,
    /// both of which must produce a <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="TResult">The output type produced by both branches.</typeparam>
    /// <param name="result">The union to match.</param>
    /// <param name="onOk">Invoked with the success value when the union is successful.</param>
    /// <param name="onError">Invoked with the error when the union has failed.</param>
    public static TResult Match<T, TResult>(
        this (T Value, UnionError? Error) result,
        Func<T, TResult> onOk,
        Func<UnionError, TResult> onError)
    {
        ArgumentNullException.ThrowIfNull(onOk);
        ArgumentNullException.ThrowIfNull(onError);
        
        return result.Error is null ? onOk(result.Value) : onError(result.Error);
    }
}
