namespace UnionRailway;

/// <summary>
/// Convenience extension methods for creating <see cref="Rail{T}"/> failures
/// directly from <see cref="UnionError"/> values.
/// </summary>
public static class UnionErrorExtensions
{
    /// <summary>
    /// Converts a <see cref="UnionError"/> to a failed <see cref="Rail{T}"/>.
    /// <code>
    /// Rail&lt;Order&gt; result = new UnionError.NotFound("Order").ToFail&lt;Order&gt;();
    /// </code>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rail<T> ToFail<T>(this UnionError error) => Union.Fail<T>(error);
}
