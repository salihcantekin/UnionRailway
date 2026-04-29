using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace UnionRailway.AspNetCore;

/// <summary>
/// Extension methods for applying <see cref="RailEndpointFilter"/> to route groups
/// and individual endpoints.
/// </summary>
public static class RailwayEndpointExtensions
{
    /// <summary>
    /// Adds <see cref="RailEndpointFilter"/> to all endpoints in this group,
    /// enabling automatic <see cref="Rail{T}"/> → <c>IResult</c> conversion.
    /// </summary>
    /// <example>
    /// <code>
    /// var api = app.MapGroup("/api").WithRailwayFilter();
    ///
    /// api.MapGet("/users/{id}", async (int id, UserService svc) =>
    ///     await svc.GetUserAsync(id)); // No .ToHttpResult() needed!
    /// </code>
    /// </example>
    public static RouteGroupBuilder WithRailwayFilter(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return group.AddEndpointFilter<RailEndpointFilter>();
    }

    /// <summary>
    /// Adds <see cref="RailEndpointFilter"/> to this specific endpoint.
    /// </summary>
    public static RouteHandlerBuilder WithRailwayFilter(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter<RailEndpointFilter>();
    }
}
