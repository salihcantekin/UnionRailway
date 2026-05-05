using System.Net;
using System.Net.Http.Json;
using Shouldly;
using UnionRailway.Demo;
using Xunit;

namespace UnionRailway.IntegrationTests;

public class Step16IntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly System.Net.Http.HttpClient _client;

    public Step16IntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── Ensure ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Ensure_ProductInStock_ShouldReturn200()
    {
        var response = await _client.GetAsync("/demo/step16/ensure/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task Ensure_ProductOutOfStock_ShouldReturn409()
    {
        var response = await _client.GetAsync("/demo/step16/ensure/3");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(409);
    }

    [Fact]
    public async Task Ensure_ProductNotFound_ShouldReturn404()
    {
        var response = await _client.GetAsync("/demo/step16/ensure/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Ensure + Bind chain ─────────────────────────────────────────────

    [Fact]
    public async Task EnsureChain_ExistingProduct_ShouldReturn200WithLabel()
    {
        var response = await _client.GetAsync("/demo/step16/ensure-chain/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Label");
        body.ShouldContain("Laptop Pro");
    }

    [Fact]
    public async Task EnsureChain_NonExistingProduct_ShouldReturn404()
    {
        var response = await _client.GetAsync("/demo/step16/ensure-chain/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Switch ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Switch_ExistingProduct_ShouldReturn200()
    {
        var response = await _client.GetAsync("/demo/step16/switch/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task Switch_NonExistingProduct_ShouldReturn404()
    {
        var response = await _client.GetAsync("/demo/step16/switch/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── ToFail ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ToFail_ShouldReturn409()
    {
        var response = await _client.GetAsync("/demo/step16/to-fail");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(409);
    }

    // ── SystemFailure(string) ───────────────────────────────────────────

    [Fact]
    public async Task SystemFailureMessage_ShouldReturn500()
    {
        var response = await _client.GetAsync("/demo/step16/system-failure-message");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(500);
    }
}
