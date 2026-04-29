using Microsoft.Extensions.DependencyInjection;

namespace UnionRailway.AspNetCore;

/// <summary>
/// Extension methods for registering UnionRailway services in the DI container.
/// </summary>
public static class RailwayServiceCollectionExtensions
{
    /// <summary>
    /// Registers UnionRailway services including <see cref="RailwayOptions"/>
    /// and an optional <see cref="IUnionErrorMapper"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddRailway(options =>
    /// {
    ///     options.ConfigureProblem = pd =>
    ///         pd.Extensions["traceId"] = Activity.Current?.Id;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddRailway(
        this IServiceCollection services,
        Action<RailwayOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<RailwayOptions>(_ => { });
        }

        return services;
    }

    /// <summary>
    /// Registers UnionRailway services with a custom <see cref="IUnionErrorMapper"/>.
    /// </summary>
    public static IServiceCollection AddRailway<TMapper>(
        this IServiceCollection services,
        Action<RailwayOptions>? configure = null)
        where TMapper : class, IUnionErrorMapper
    {
        services.AddRailway(configure);
        services.AddSingleton<IUnionErrorMapper, TMapper>();
        return services;
    }
}
