using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using UnionRailway.AspNetCore.OpenApi;
using Xunit;

namespace UnionRailway.Tests;

public sealed class RailOpenApiExtensionsTests
{
    private static IReadOnlyList<IProducesResponseTypeMetadata> GetProducesMetadata(WebApplication app, string route)
    {
        IEndpointRouteBuilder endpointRouteBuilder = app;
        RouteEndpoint endpoint = Assert.IsType<RouteEndpoint>(
            Assert.Single(endpointRouteBuilder.DataSources.SelectMany(source => source.Endpoints), endpoint =>
                endpoint is RouteEndpoint routeEndpoint && routeEndpoint.RoutePattern.RawText == route));

        return endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().ToArray();
    }

    [Fact]
    public void WithRailOpenApi_AddsDefaultSuccessAndErrorResponses()
    {
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();
        WebApplication app = appBuilder.Build();

        RouteHandlerBuilder route = app.MapGet("/users/{id:int}", () => Union.Ok(new UserDto(1, "Alice")));
        route.WithRailOpenApi<RouteHandlerBuilder, UserDto>();

        IReadOnlyList<IProducesResponseTypeMetadata> metadata = GetProducesMetadata(app, "/users/{id:int}");

        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status200OK && m.Type == typeof(UserDto));
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status400BadRequest && m.Type == typeof(HttpValidationProblemDetails));
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status401Unauthorized && m.Type == typeof(ProblemDetails));
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status403Forbidden && m.Type == typeof(ProblemDetails));
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status404NotFound && m.Type == typeof(ProblemDetails));
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status409Conflict && m.Type == typeof(ProblemDetails));
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status500InternalServerError && m.Type == typeof(ProblemDetails));
    }

    [Fact]
    public void WithCreatedRailOpenApi_UsesCreatedSuccessStatus()
    {
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();
        WebApplication app = appBuilder.Build();

        RouteHandlerBuilder route = app.MapPost("/users", () => Union.Ok(new UserDto(1, "Alice")));
        route.WithCreatedRailOpenApi<RouteHandlerBuilder, UserDto>();

        IReadOnlyList<IProducesResponseTypeMetadata> metadata = GetProducesMetadata(app, "/users");

        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status201Created && m.Type == typeof(UserDto));
    }

    [Fact]
    public void WithRailOpenApi_WithOptions_AllowsSelectiveResponses()
    {
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();
        WebApplication app = appBuilder.Build();

        RouteHandlerBuilder route = app.MapGet("/orders/{id:int}", () => Union.Ok(new OrderDto(1, 10m)));
        route.WithRailOpenApi<RouteHandlerBuilder, OrderDto>(options =>
        {
            options.SuccessStatusCode = StatusCodes.Status202Accepted;
            options.IncludeUnauthorized = false;
            options.IncludeForbidden = false;
            options.IncludeSystemFailure = false;
        });

        IReadOnlyList<IProducesResponseTypeMetadata> metadata = GetProducesMetadata(app, "/orders/{id:int}");

        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status202Accepted && m.Type == typeof(OrderDto));
        Assert.DoesNotContain(metadata, m => m.StatusCode == StatusCodes.Status401Unauthorized);
        Assert.DoesNotContain(metadata, m => m.StatusCode == StatusCodes.Status403Forbidden);
        Assert.DoesNotContain(metadata, m => m.StatusCode == StatusCodes.Status500InternalServerError);
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status400BadRequest);
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status404NotFound);
        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status409Conflict);
    }

    [Fact]
    public void WithRailOpenApi_WithExplicitSuccessStatus_UsesProvidedValue()
    {
        WebApplicationBuilder appBuilder = WebApplication.CreateBuilder();
        WebApplication app = appBuilder.Build();

        RouteHandlerBuilder route = app.MapGet("/products/{id:int}", () => Union.Ok(new ProductDto(1, "Widget")));
        route.WithRailOpenApi<RouteHandlerBuilder, ProductDto>(StatusCodes.Status206PartialContent);

        IReadOnlyList<IProducesResponseTypeMetadata> metadata = GetProducesMetadata(app, "/products/{id:int}");

        Assert.Contains(metadata, m => m.StatusCode == StatusCodes.Status206PartialContent && m.Type == typeof(ProductDto));
    }

    private sealed record UserDto(int Id, string Name);
    private sealed record OrderDto(int Id, decimal Total);
    private sealed record ProductDto(int Id, string Name);
}
