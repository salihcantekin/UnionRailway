using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using UnionRailway;
using UnionRailway.AspNetCore;
using HttpClient = System.Net.Http.HttpClient;

namespace UnionRailway.Tests;

public sealed class RailwayExceptionMiddlewareTests : IAsyncDisposable
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
                    services.AddLogging();
                    services.AddRailway();
                    configureServices?.Invoke(services);
                });
                web.Configure(app =>
                {
                    app.UseRailwayExceptionHandler();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/throw", (HttpContext _) =>
                        {
                            throw new InvalidOperationException("Something broke");
#pragma warning disable CS0162
                            return Task.CompletedTask;
#pragma warning restore CS0162
                        });

                        endpoints.MapGet("/ok", () => Results.Ok("works"));
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

    [Fact]
    public async Task UnhandledException_Returns500ProblemDetails()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var pd = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Internal Server Error", pd!.Title);
    }

    [Fact]
    public async Task NormalRequest_PassesThrough()
    {
        var client = await CreateClientAsync();

        var response = await client.GetAsync("/ok");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WithGlobalConfigureProblem_EnrichesExceptionResponse()
    {
        var client = await CreateClientAsync(services =>
        {
            services.Configure<RailwayOptions>(options =>
            {
                options.ConfigureProblem = pd =>
                {
                    pd.Detail = "Sanitized error.";
                    pd.Extensions["support"] = "contact@example.com";
                };
            });
        });

        var response = await client.GetAsync("/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sanitized error.", body);
        Assert.Contains("support", body);
    }

    [Fact]
    public async Task WithMapper_ExceptionStillUsesSystemFailureMapping()
    {
        var client = await CreateClientAsync(services =>
        {
            services.AddSingleton<IUnionErrorMapper, SystemFailureMapper>();
        });

        var response = await client.GetAsync("/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var pd = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Custom System Error", pd!.Title);
    }

    private sealed class SystemFailureMapper : IUnionErrorMapper
    {
        public IResult? TryMap(UnionError error) => error.Value switch
        {
            UnionError.SystemFailure => Results.Problem(
                detail: "A system error occurred.",
                statusCode: 500,
                title: "Custom System Error"),
            _ => null
        };
    }
}
