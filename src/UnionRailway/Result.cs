namespace UnionRailway;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// This is the core return type for Railway-Oriented Programming in C#.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public abstract record Result<T>
{
    private Result() { }

    /// <summary>
    /// Represents a successful result containing the data.
    /// </summary>
    /// <param name="Data">The success value.</param>
    public sealed record Ok(T Data) : Result<T>;

    /// <summary>
    /// Represents a failed result containing the error.
    /// </summary>
    /// <param name="Err">The error describing what went wrong.</param>
    public sealed record Error(UnionError Err) : Result<T>;
}
