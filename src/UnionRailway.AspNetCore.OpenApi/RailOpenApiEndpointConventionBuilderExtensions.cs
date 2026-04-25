namespace UnionRailway.AspNetCore.OpenApi;

/// <summary>
/// Adds OpenAPI response metadata conventions for Minimal API endpoints that return <see cref="Rail{T}"/> values.
/// </summary>
public static class RailOpenApiEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds the standard success and error metadata set for a <see cref="Rail{T}"/> endpoint.
    /// </summary>
    public static TBuilder WithRailOpenApi<TBuilder, TSuccess>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithRailOpenApi<TBuilder, TSuccess>(RailOpenApiOptions.Default);
    }

    /// <summary>
    /// Adds metadata for a created rail response and the standard error metadata set.
    /// </summary>
    public static TBuilder WithCreatedRailOpenApi<TBuilder, TSuccess>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithRailOpenApi<TBuilder, TSuccess>(RailOpenApiOptions.Created);
    }

    /// <summary>
    /// Adds metadata for a <see cref="Rail{T}"/> endpoint using a custom success status code.
    /// </summary>
    public static TBuilder WithRailOpenApi<TBuilder, TSuccess>(this TBuilder builder, int successStatusCode)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        RailOpenApiOptions options = RailOpenApiOptions.Default.Clone();
        options.SuccessStatusCode = successStatusCode;
        return builder.WithRailOpenApi<TBuilder, TSuccess>(options);
    }

    /// <summary>
    /// Adds metadata for a <see cref="Rail{T}"/> endpoint using customizable options.
    /// </summary>
    public static TBuilder WithRailOpenApi<TBuilder, TSuccess>(
        this TBuilder builder,
        Action<RailOpenApiOptions> configure)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        RailOpenApiOptions options = RailOpenApiOptions.Default.Clone();
        configure(options);
        return builder.WithRailOpenApi<TBuilder, TSuccess>(options);
    }

    private static TBuilder WithRailOpenApi<TBuilder, TSuccess>(
        this TBuilder builder,
        RailOpenApiOptions options)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.Add(endpointBuilder =>
        {
            AddSuccessMetadata<TSuccess>(endpointBuilder.Metadata, options.SuccessStatusCode);

            if (options.IncludeValidation)
            {
                endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(
                    StatusCodes.Status400BadRequest,
                    typeof(HttpValidationProblemDetails),
                    ["application/problem+json"]));
            }

            if (options.IncludeUnauthorized)
            {
                endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(
                    StatusCodes.Status401Unauthorized,
                    typeof(ProblemDetails),
                    ["application/problem+json"]));
            }

            if (options.IncludeForbidden)
            {
                endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(
                    StatusCodes.Status403Forbidden,
                    typeof(ProblemDetails),
                    ["application/problem+json"]));
            }

            if (options.IncludeNotFound)
            {
                endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(
                    StatusCodes.Status404NotFound,
                    typeof(ProblemDetails),
                    ["application/problem+json"]));
            }

            if (options.IncludeConflict)
            {
                endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(
                    StatusCodes.Status409Conflict,
                    typeof(ProblemDetails),
                    ["application/problem+json"]));
            }

            if (options.IncludeSystemFailure)
            {
                endpointBuilder.Metadata.Add(new ProducesResponseTypeMetadata(
                    StatusCodes.Status500InternalServerError,
                    typeof(ProblemDetails),
                    ["application/problem+json"]));
            }
        });

        return builder;
    }

    private static void AddSuccessMetadata<TSuccess>(IList<object> metadata, int successStatusCode)
    {
        metadata.Add(new ProducesResponseTypeMetadata(
            successStatusCode,
            typeof(TSuccess),
            ["application/json"]));
    }
}
