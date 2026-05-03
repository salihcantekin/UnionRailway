using System.Net;
using System.Net.Http.Json;
using Shouldly;
using UnionRailway.Demo;
using Xunit;

namespace UnionRailway.IntegrationTests;

public class Step05IntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly System.Net.Http.HttpClient _client;

    public Step05IntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Map_ExistingProduct_ShouldReturn200WithTransformedPrice()
    {
        var response = await _client.GetAsync("/demo/step05/map/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        result.ShouldNotBeNull();
        result.ShouldContainKey("id");
        result.ShouldContainKey("name");
        result.ShouldContainKey("priceWithTax");
    }

    [Fact]
    public async Task Map_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step05/map/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task Bind_ProductInStock_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step05/bind/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Debug to see actual response
        var content = await response.Content.ReadAsStringAsync();
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        // Try both PascalCase and camelCase
        bool hasStatus = json.TryGetProperty("status", out var statusLower) || 
                        json.TryGetProperty("Status", out var statusUpper);

        hasStatus.ShouldBeTrue($"Response: {content}");
    }

    [Fact]
    public async Task Bind_ProductOutOfStock_ShouldReturn409Conflict()
    {
        var response = await _client.GetAsync("/demo/step05/bind/3");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(409);
        problemDetails.Detail.ShouldContain("Out of stock");
    }

    [Fact]
    public async Task Bind_NonExistingProduct_ShouldReturn404NotFound()
    {
        var response = await _client.GetAsync("/demo/step05/bind/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task Chain_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step05/chain/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        result.ShouldNotBeNull();
        result.ShouldContainKey("id");
        result.ShouldContainKey("name");
        result.ShouldContainKey("sku");
        result.ShouldContainKey("stock");
    }

    [Fact]
    public async Task Chain_NonExistingProduct_ShouldReturn404NotFound()
    {
        var response = await _client.GetAsync("/demo/step05/chain/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task OneLiner_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step05/oneliner/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
