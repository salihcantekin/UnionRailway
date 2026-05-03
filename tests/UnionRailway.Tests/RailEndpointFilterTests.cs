using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using UnionRailway;
using UnionRailway.AspNetCore;
using HttpClient = System.Net.Http.HttpClient;

namespace UnionRailway.Tests;

public sealed class RailEndpointFilterTests : IAsyncDisposable
{
    private IHost? _host;
    private System.Net.Http.HttpClient? _client;

    private async Task<System.Net.Http.HttpClient> CreateClientAsync(
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddRailway();
                    configureServices?.Invoke(services);

                    // Workaround for .NET 11 TestHost PipeWriter.UnflushedBytes bug
                    services.AddTransient<IStartupFilter, TestHostBugWorkaroundStartupFilter>();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/ok", () => Union.Ok(new { Name = "Alice" }))
                            .WithRailwayFilter();

                        endpoints.MapGet("/not-found", () => Union.Fail<object>(new UnionError.NotFound("User")))
                            .WithRailwayFilter();

                        endpoints.MapGet("/unit", () => Union.Ok())
                            .WithRailwayFilter();

                        endpoints.MapGet("/custom-error", () =>
                            Union.Fail<object>(new UnionError.Custom("RATE_LIMIT", "Too many", StatusCode: 429)))
                            .WithRailwayFilter();

                        endpoints.MapGet("/no-filter", () => Union.Ok(new { Name = "Raw" }));

                        endpoints.MapGet("/plain-result", () => Results.Ok("plain"))
                            .WithRailwayFilter();
                    });
                });
            });

        _host = await builder.StartAsync();
        _client = _host.GetTestClient();
        return _client;
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    // ── Success path ────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_RailOk_Returns200WithBody()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/ok");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("Alice", body!["name"]);
    }

    // ── Error path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_RailNotFound_Returns404ProblemDetails()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var pd = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Not Found", pd!.Title);
    }

    // ── Unit → NoContent ────────────────────────────────────────────────

    [Fact]
    public async Task Filter_RailUnit_Returns204()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/unit");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Custom error ────────────────────────────────────────────────────

    [Fact]
    public async Task Filter_RailCustomError_ReturnsCustomStatusCode()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/custom-error");

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
    }

    // ── Non-Rail return passes through ──────────────────────────────────

    [Fact]
    public async Task Filter_PlainIResult_PassesThrough()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/plain-result");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── With IUnionErrorMapper from DI ──────────────────────────────────

    [Fact]
    public async Task Filter_WithMapperFromDI_UsesCustomMapping()
    {
        var client = await CreateClientAsync(services =>
        {
            services.AddSingleton<IUnionErrorMapper, TestMapper>();
        });

        var response = await client.GetAsync("/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var pd = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Custom Not Found", pd!.Title);
    }

    // ── With global RailwayOptions ──────────────────────────────────────

    [Fact]
    public async Task Filter_WithGlobalConfigureProblem_EnrichesResponse()
    {
        var client = await CreateClientAsync(services =>
        {
            services.Configure<RailwayOptions>(options =>
            {
                options.ConfigureProblem = pd => pd.Extensions["global"] = "enriched";
            });
        });

        var response = await client.GetAsync("/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("global", body);
    }

    private sealed class TestMapper : IUnionErrorMapper
    {
        public IResult? TryMap(UnionError error) => error.Value switch
        {
            UnionError.NotFound nf => Results.Problem(
                detail: $"Could not find '{nf.Resource}'.",
                statusCode: 404,
                title: "Custom Not Found"),
            _ => null
        };
    }
}
