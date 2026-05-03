using System.Net;
using System.Net.Http.Json;
using Shouldly;
using UnionRailway.Demo;
using UnionRailway.Demo.Services;
using Xunit;

namespace UnionRailway.IntegrationTests;

public class Step06IntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly System.Net.Http.HttpClient _client;

    public Step06IntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Tap_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step06/tap/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task Tap_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step06/tap/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task Recover_ExistingProduct_ShouldReturnRealProduct()
    {
        var response = await _client.GetAsync("/demo/step06/recover/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
        product.Name.ShouldNotBe("Guest Placeholder");
    }

    [Fact]
    public async Task Recover_NonExistingProduct_ShouldReturnFallbackProduct()
    {
        var response = await _client.GetAsync("/demo/step06/recover/999");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(0);
        product.Name.ShouldBe("Guest Placeholder");
        product.Sku.ShouldBe("N/A");
    }

    [Fact]
    public async Task TapAndRecover_ExistingProduct_ShouldReturnRealProduct()
    {
        var response = await _client.GetAsync("/demo/step06/tap-and-recover/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
        product.Name.ShouldNotBe("Default Product");
    }

    [Fact]
    public async Task TapAndRecover_NonExistingProduct_ShouldReturnFallbackProduct()
    {
        var response = await _client.GetAsync("/demo/step06/tap-and-recover/999");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(-1);
        product.Name.ShouldBe("Default Product");
        product.Sku.ShouldBe("DEFAULT");
    }
}
