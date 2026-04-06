namespace UnionRailway;

/// <summary>
/// A closed union of every error category an operation can produce.
/// The shape follows the custom union pattern so it can migrate naturally to
/// native C# union support.
/// <code>
/// UnionError error = new UnionError.NotFound("User");
///
/// var message = error.Value switch
/// {
///     UnionError.NotFound nf      = $"'{nf.Resource}' not found",
///     UnionError.Conflict c       = $"Conflict: {c.Reason}",
///     UnionError.Unauthorized     = "Authentication required",
///     UnionError.Forbidden f      = $"Access denied: {f.Reason}",
///     UnionError.Validation v     = $"{v.Fields.Count} field(s) invalid",
///     UnionError.SystemFailure sf = sf.Ex.Message,
///     null                        = "Unknown error"
/// };
/// </code>
/// </summary>
#if NET11_0_OR_GREATER
public union UnionError(
    UnionError.NotFound,
    UnionError.Conflict,
    UnionError.Unauthorized,
    UnionError.Forbidden,
    UnionError.Validation,
    UnionError.SystemFailure)
{
    /// <summary>Returns <see langword="true"/> when the union is the default value.</summary>
    public bool IsDefault => Value is null;

    /// <summary>Attempts to read the underlying case as <typeparamref name="TCase"/>.</summary>
    public bool TryGet<TCase>([NotNullWhen(true)] out TCase? error)
        where TCase : class
    {
        error = Value as TCase;
        return error is not null;
    }

    // ── Case types ──────────────────────────────────────────────────────────

    /// <summary>The requested resource was not found.</summary>
    /// <param name="Resource">Name or identifier of the missing resource.</param>
    public sealed record NotFound(string Resource);

    /// <summary>The operation conflicts with existing state (e.g., duplicate key).</summary>
    /// <param name="Reason">Human-readable explanation of the conflict.</param>
    public sealed record Conflict(string Reason);

    /// <summary>The caller is not authenticated.</summary>
    public sealed record Unauthorized();

    /// <summary>The caller is authenticated but lacks permission for this operation.</summary>
    /// <param name="Reason">Human-readable explanation of why access was denied.</param>
    public sealed record Forbidden(string Reason);

    /// <summary>One or more input fields failed validation.</summary>
    /// <param name="Fields">Per-field error messages keyed by field name.</param>
    public sealed record Validation(IReadOnlyDictionary<string, string[]> Fields);

    /// <summary>An unexpected system-level failure occurred.</summary>
    /// <param name="Ex">The originating exception.</param>
    public sealed record SystemFailure(Exception Ex);

    // ── Validation constructors (overloads on the Validation record) ──────────

    /// <summary>Creates a <see cref="Validation"/> error from a field-errors dictionary.</summary>
    public static UnionError CreateValidation(IDictionary<string, string[]> fields) =>
        new Validation(new ReadOnlyDictionary<string, string[]>(
            new Dictionary<string, string[]>(fields, StringComparer.Ordinal)));

    /// <summary>
    /// Creates a <see cref="Validation"/> error from field/message tuple pairs:
    /// <code>UnionError.CreateValidation([("Email", ["Invalid"]), ("Name", ["Required"])])</code>
    /// </summary>
    public static UnionError CreateValidation(IEnumerable<(string Field, string[] Messages)> pairs) =>
        new Validation(new ReadOnlyDictionary<string, string[]>(
            pairs.ToDictionary(p => p.Field, p => p.Messages, StringComparer.Ordinal)));
}
#else
[System.Runtime.CompilerServices.Union]
public readonly struct UnionError : IEquatable<UnionError>, System.Runtime.CompilerServices.IUnion
{
    private readonly object? value;

    /// <summary>Gets the underlying case value.</summary>
    public object? Value => value;

    public UnionError(NotFound value) => this.value = value;

    public UnionError(Conflict value) => this.value = value;

    public UnionError(Unauthorized value) => this.value = value;

    public UnionError(Forbidden value) => this.value = value;

    public UnionError(Validation value) => this.value = value;

    public UnionError(SystemFailure value) => this.value = value;

    /// <summary>Returns <see langword="true"/> when the union is the default value.</summary>
    public bool IsDefault => value is null;

    /// <summary>Attempts to read the underlying case as <typeparamref name="TCase"/>.</summary>
    public bool TryGet<TCase>([NotNullWhen(true)] out TCase? error)
        where TCase : class
    {
        error = value as TCase;
        return error is not null;
    }

    public static implicit operator UnionError(NotFound value) => new(value);

    public static implicit operator UnionError(Conflict value) => new(value);

    public static implicit operator UnionError(Unauthorized value) => new(value);

    public static implicit operator UnionError(Forbidden value) => new(value);

    public static implicit operator UnionError(Validation value) => new(value);

    public static implicit operator UnionError(SystemFailure value) => new(value);

    public bool Equals(UnionError other) => Equals(value, other.value);

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is UnionError other && Equals(other);

    public override int GetHashCode() => value?.GetHashCode() ?? 0;

    public override string ToString() => value?.ToString() ?? nameof(UnionError);

    // ── Case types ──────────────────────────────────────────────────────────

    /// <summary>The requested resource was not found.</summary>
    /// <param name="Resource">Name or identifier of the missing resource.</param>
    public sealed record NotFound(string Resource);

    /// <summary>The operation conflicts with existing state (e.g., duplicate key).</summary>
    /// <param name="Reason">Human-readable explanation of the conflict.</param>
    public sealed record Conflict(string Reason);

    /// <summary>The caller is not authenticated.</summary>
    public sealed record Unauthorized();

    /// <summary>The caller is authenticated but lacks permission for this operation.</summary>
    /// <param name="Reason">Human-readable explanation of why access was denied.</param>
    public sealed record Forbidden(string Reason);

    /// <summary>One or more input fields failed validation.</summary>
    /// <param name="Fields">Per-field error messages keyed by field name.</param>
    public sealed record Validation(IReadOnlyDictionary<string, string[]> Fields);

    /// <summary>An unexpected system-level failure occurred.</summary>
    /// <param name="Ex">The originating exception.</param>
    public sealed record SystemFailure(Exception Ex);

    // ── Validation constructors (overloads on the Validation record) ──────────

    /// <summary>Creates a <see cref="Validation"/> error from a field-errors dictionary.</summary>
    public static UnionError CreateValidation(IDictionary<string, string[]> fields) =>
        new Validation(new ReadOnlyDictionary<string, string[]>(
            new Dictionary<string, string[]>(fields, StringComparer.Ordinal)));

    /// <summary>
    /// Creates a <see cref="Validation"/> error from field/message tuple pairs:
    /// <code>UnionError.CreateValidation([("Email", ["Invalid"]), ("Name", ["Required"])])</code>
    /// </summary>
    public static UnionError CreateValidation(IEnumerable<(string Field, string[] Messages)> pairs) =>
        new Validation(new ReadOnlyDictionary<string, string[]>(
            pairs.ToDictionary(p => p.Field, p => p.Messages, StringComparer.Ordinal)));
}
#endif
