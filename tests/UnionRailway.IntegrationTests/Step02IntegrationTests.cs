using System.Net;
using System.Net.Http.Json;
using Shouldly;
using UnionRailway.Demo;
using UnionRailway.Demo.Services;
using Xunit;

namespace UnionRailway.IntegrationTests;

public class Step02IntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly System.Net.Http.HttpClient _client;

    public Step02IntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step02/products/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetProductById_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step02/products/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step02/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.ShouldNotBeNull();
        products.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetProductByIdExplicit_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step02/products/1/explicit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetProductByIdExplicit_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step02/products/999/explicit");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }
}
