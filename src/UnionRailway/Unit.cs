namespace UnionRailway;

/// <summary>
/// Represents a successful operation that has no meaningful return value.
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// Returns the singleton value.
    /// </summary>
    public static readonly Unit Value = default;
}
