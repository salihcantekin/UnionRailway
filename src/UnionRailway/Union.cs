namespace UnionRailway;

/// <summary>
/// Static helpers for constructing anonymous union tuples
/// <c>(T Value, UnionError? Error)</c> — the zero-allocation discriminated-union
/// type used throughout UnionRailway.
/// </summary>
/// <remarks>
/// <para>
/// <c>(T Value, UnionError? Error)</c> is a <see cref="System.ValueTuple{T1,T2}"/> — a plain
/// stack-allocated struct. No extra heap objects are created compared to returning
/// the value directly.
/// </para>
/// <para>
/// Call site pattern:
/// <code>
/// // Returning a success:
/// return Union.Ok(user);
///
/// // Returning an error:
/// return Union.Fail&lt;User&gt;(new UnionError.NotFound("User"));
///
/// // Or use tuple literals directly (both are equivalent):
/// return (user, null);
/// return (default!, new UnionError.NotFound("User"));
/// </code>
/// </para>
/// </remarks>
public static class Union
{
    /// <summary>Creates a success union carrying <paramref name="value"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (T Value, UnionError? Error) Ok<T>(T value) => (value, null);

    /// <summary>Creates a failure union carrying <paramref name="error"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (T Value, UnionError? Error) Fail<T>(UnionError error) => (default!, error);

    /// <summary>
    /// Combines two union values into one. Returns the first error encountered,
    /// or a pair of both success values.
    /// </summary>
    public static ((T1 First, T2 Second) Value, UnionError? Error) Combine<T1, T2>(
        (T1 Value, UnionError? Error) first,
        (T2 Value, UnionError? Error) second)
    {
        if (first.Error is not null)  
            return (default, first.Error);
        
        if (second.Error is not null) 
            return (default, second.Error);
        
        return ((first.Value, second.Value), null);
    }
}

