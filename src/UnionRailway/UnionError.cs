namespace UnionRailway;

/// <summary>
/// Represents a closed set of domain error types following the Railway-Oriented Programming pattern.
/// Use pattern matching with switch expressions to handle each error case exhaustively.
/// </summary>
public abstract record UnionError
{
    private UnionError() { }

    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    /// <param name="Resource">A description of the resource that was not found.</param>
    public sealed record NotFound(string Resource) : UnionError;

    /// <summary>
    /// A conflict occurred, such as a duplicate or concurrency violation.
    /// </summary>
    /// <param name="Reason">A description of the conflict.</param>
    public sealed record Conflict(string Reason) : UnionError;

    /// <summary>
    /// The caller is not authorized to perform the requested operation.
    /// </summary>
    public sealed record Unauthorized() : UnionError;

    /// <summary>
    /// One or more validation errors occurred.
    /// </summary>
    /// <param name="Fields">A dictionary mapping field names to their validation error messages.</param>
    public sealed record Validation(IReadOnlyDictionary<string, string> Fields) : UnionError;

    /// <summary>
    /// An unexpected system failure occurred.
    /// </summary>
    /// <param name="Ex">The underlying exception that caused the failure.</param>
    public sealed record SystemFailure(Exception Ex) : UnionError;
}
