namespace UnionRailway.AspNetCore.OpenApi;

/// <summary>
/// Configures which OpenAPI responses are advertised for <see cref="Rail{T}"/> endpoints.
/// </summary>
public sealed class RailOpenApiOptions
{
    /// <summary>The success status code used for the primary response.</summary>
    public int SuccessStatusCode { get; set; } = StatusCodes.Status200OK;

    /// <summary>Gets or sets whether a 400 validation response should be included.</summary>
    public bool IncludeValidation { get; set; } = true;

    /// <summary>Gets or sets whether a 401 unauthorized response should be included.</summary>
    public bool IncludeUnauthorized { get; set; } = true;

    /// <summary>Gets or sets whether a 403 forbidden response should be included.</summary>
    public bool IncludeForbidden { get; set; } = true;

    /// <summary>Gets or sets whether a 404 not found response should be included.</summary>
    public bool IncludeNotFound { get; set; } = true;

    /// <summary>Gets or sets whether a 409 conflict response should be included.</summary>
    public bool IncludeConflict { get; set; } = true;

    /// <summary>Gets or sets whether a 500 problem response should be included.</summary>
    public bool IncludeSystemFailure { get; set; } = true;

    /// <summary>
    /// Creates default options for a standard OK response.
    /// </summary>
    public static RailOpenApiOptions Default { get; } = new();

    /// <summary>
    /// Creates default options for a created response.
    /// </summary>
    public static RailOpenApiOptions Created { get; } = new() { SuccessStatusCode = StatusCodes.Status201Created };

    internal RailOpenApiOptions Clone() => new()
    {
        SuccessStatusCode = SuccessStatusCode,
        IncludeValidation = IncludeValidation,
        IncludeUnauthorized = IncludeUnauthorized,
        IncludeForbidden = IncludeForbidden,
        IncludeNotFound = IncludeNotFound,
        IncludeConflict = IncludeConflict,
        IncludeSystemFailure = IncludeSystemFailure
    };
}
