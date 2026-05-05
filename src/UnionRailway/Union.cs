namespace UnionRailway;

/// <summary>
/// Static helpers for constructing and combining <see cref="Rail{T}"/> values.
/// </summary>
public static class Union
{
    /// <summary>Creates a successful rail carrying <paramref name="value"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rail<T> Ok<T>(T value) => value;

    /// <summary>
    /// Creates a successful rail carrying a <paramref name="value"/> boxed as <see cref="object"/>.
    /// This overload prevents generic type inference issues when working with anonymous types.
    /// </summary>
    /// <param name="value">The value to wrap, typically an anonymous type or pre-boxed object.</param>
    /// <returns>A <see cref="Rail{T}"/> where T is <see cref="object"/>.</returns>
    /// <remarks>
    /// <para><b>When to use this:</b></para>
    /// <list type="bullet">
    /// <item>Returning anonymous types (DTOs) from <see cref="UnionExtensions.BindObject{T}(Rail{T}, Func{T, Rail{object}})"/></item>
    /// <item>Working with heterogeneous collections of Rail results</item>
    /// <item>Explicit boxing for HTTP response serialization</item>
    /// </list>
    /// <para><b>Example:</b></para>
    /// <code>
    /// Rail&lt;object&gt; result = Union.Ok(new { Id = 1, Name = "Product" });
    /// // No generic type inference issues, explicit Rail&lt;object&gt; return type
    /// </code>
    /// <para><b>Performance Note:</b></para>
    /// <para>
    /// This method boxes the value. Prefer strongly-typed <see cref="Ok{T}(T)"/> when possible 
    /// and only use this at HTTP/serialization boundaries for DTOs.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rail<object> Ok(object value) => value;

    /// <summary>Creates a failed rail carrying <paramref name="error"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rail<T> Fail<T>(UnionError error) => error;

    /// <summary>Creates a successful rail for operations with no meaningful value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rail<Unit> Ok() => Unit.Value;

    /// <summary>
    /// Combines two rails into one. Returns the first error encountered,
    /// or a pair of both success values.
    /// </summary>
    public static Rail<(T1 First, T2 Second)> Combine<T1, T2>(
        Rail<T1> first,
        Rail<T2> second)
    {
        if (first.TryGetError(out UnionError? firstError))
        {
            return firstError.GetValueOrDefault();
        }

        if (second.TryGetError(out UnionError? secondError))
        {
            return secondError.GetValueOrDefault();
        }

        return (first.Unwrap(), second.Unwrap());
    }

    /// <summary>
    /// Combines three rails into one. Returns the first error encountered,
    /// or a triple of all success values.
    /// </summary>
    public static Rail<(T1 First, T2 Second, T3 Third)> Combine<T1, T2, T3>(
        Rail<T1> first,
        Rail<T2> second,
        Rail<T3> third)
    {
        if (first.TryGetError(out UnionError? firstError))
        {
            return firstError.GetValueOrDefault();
        }

        if (second.TryGetError(out UnionError? secondError))
        {
            return secondError.GetValueOrDefault();
        }

        if (third.TryGetError(out UnionError? thirdError))
        {
            return thirdError.GetValueOrDefault();
        }

        return (first.Unwrap(), second.Unwrap(), third.Unwrap());
    }
}
