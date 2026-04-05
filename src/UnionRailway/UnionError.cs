using System.Collections.ObjectModel;

namespace UnionRailway;

/// <summary>
/// A closed discriminated union of every error category an operation can produce.
/// Use the nested record cases as type-safe variants and pattern-match exhaustively
/// with <c>switch</c> or <c>is</c>:
/// <code>
/// var message = error switch
/// {
///     UnionError.NotFound nf      =&gt; $"'{nf.Resource}' not found",
///     UnionError.Conflict c       =&gt; $"Conflict: {c.Reason}",
///     UnionError.Unauthorized     =&gt; "Authentication required",
///     UnionError.Forbidden f      =&gt; $"Access denied: {f.Reason}",
///     UnionError.Validation v     =&gt; $"{v.Fields.Count} field(s) invalid",
///     UnionError.SystemFailure sf =&gt; sf.Ex.Message,
///     _                           =&gt; "Unknown error"
/// };
/// </code>
/// The <see langword="private protected"/> constructor seals this hierarchy — only
/// the nested cases defined here can extend <see cref="UnionError"/>.
/// </summary>
public abstract record UnionError
{
    private protected UnionError() { }

    // ── Case types ──────────────────────────────────────────────────────────

    /// <summary>The requested resource was not found.</summary>
    /// <param name="Resource">Name or identifier of the missing resource.</param>
    public sealed record NotFound(string Resource) : UnionError;

    /// <summary>The operation conflicts with existing state (e.g., duplicate key).</summary>
    /// <param name="Reason">Human-readable explanation of the conflict.</param>
    public sealed record Conflict(string Reason) : UnionError;

    /// <summary>The caller is not authenticated.</summary>
    public sealed record Unauthorized() : UnionError;

    /// <summary>The caller is authenticated but lacks permission for this operation.</summary>
    /// <param name="Reason">Human-readable explanation of why access was denied.</param>
    public sealed record Forbidden(string Reason) : UnionError;

    /// <summary>One or more input fields failed validation.</summary>
    /// <param name="Fields">Per-field error messages keyed by field name.</param>
    public sealed record Validation(IReadOnlyDictionary<string, string[]> Fields) : UnionError;

    /// <summary>An unexpected system-level failure occurred.</summary>
    /// <param name="Ex">The originating exception.</param>
    public sealed record SystemFailure(Exception Ex) : UnionError;

    // ── Validation constructors (overloads on the Validation record) ──────────

    /// <summary>Creates a <see cref="Validation"/> error from a field-errors dictionary.</summary>
    public static Validation CreateValidation(IDictionary<string, string[]> fields) =>
        new(new ReadOnlyDictionary<string, string[]>(
                new Dictionary<string, string[]>(fields, StringComparer.Ordinal)));

    /// <summary>
    /// Creates a <see cref="Validation"/> error from field/message tuple pairs:
    /// <code>UnionError.CreateValidation([("Email", ["Invalid"]), ("Name", ["Required"])])</code>
    /// </summary>
    public static Validation CreateValidation(IEnumerable<(string Field, string[] Messages)> pairs) =>
        new(pairs.ToDictionary(p => p.Field, p => p.Messages, StringComparer.Ordinal)
                 .AsReadOnly());
}

// ── Kept for source-compatible access to former UnionErrorKind usages ────────
// Remove in the next major version; pattern-match on UnionError subtypes instead.
[Obsolete("Match directly on the UnionError subtype instead of comparing Kind values.")]
public enum UnionErrorKind
{
    /// <summary>A requested resource was not found.</summary>
    NotFound,

    /// <summary>Operation conflict.</summary>
    Conflict,
    /// <summary>Caller not authenticated.</summary>
    Unauthorized,
    /// <summary>Caller lacks permission.</summary>
    Forbidden,
    /// <summary>Input validation failed.</summary>
    Validation,
    /// <summary>Unexpected system failure.</summary>
    SystemFailure
}
