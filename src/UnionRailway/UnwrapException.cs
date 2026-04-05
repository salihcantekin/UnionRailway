namespace UnionRailway;

/// <summary>
/// Exception thrown by <see cref="UnionExtensions.Unwrap{T}"/> when the union
/// carries an error instead of a success value.
/// </summary>
public sealed class UnwrapException : InvalidOperationException
{
    /// <summary>The error that caused the unwrap to fail.</summary>
    public UnionError Error { get; }

    internal UnwrapException(UnionError error)
        : base($"Cannot unwrap a failed union. Error: {error}")
    {
        Error = error;
    }
}
