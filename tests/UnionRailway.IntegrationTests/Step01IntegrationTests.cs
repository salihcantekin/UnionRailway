using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using UnionRailway.Demo;
using UnionRailway.Demo.Services;
using Xunit;

namespace UnionRailway.IntegrationTests;

public class Step01IntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly System.Net.Http.HttpClient _client;

    public Step01IntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step01/products/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetProductById_NonExistingProduct_ShouldReturn404NotFound()
    {
        var response = await _client.GetAsync("/demo/step01/products/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        error.ShouldNotBeNull();
        error.ShouldContainKey("error");
    }

    [Fact]
    public async Task GetProductById_ZeroId_ShouldReturn404NotFound()
    {
        var response = await _client.GetAsync("/demo/step01/products/0");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductBySku_ExistingSku_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step01/products/by-sku/LAP-001");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Sku.ShouldBe("LAP-001");
    }

    [Fact]
    public async Task GetProductBySku_UnknownSku_ShouldReturn404NotFound()
    {
        var response = await _client.GetAsync("/demo/step01/products/by-sku/UNKNOWN");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_ValidData_ShouldReturn201Created()
    {
        var request = new CreateProductRequest(
            Name: "Test Product",
            Sku: "TEST-001",
            Price: 99.99m,
            Stock: 10
        );

        var response = await _client.PostAsJsonAsync("/demo/step01/products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Name.ShouldBe(request.Name);
        product.Sku.ShouldBe(request.Sku);
    }

    [Fact]
    public async Task CreateProduct_EmptyName_ShouldReturn400BadRequest()
    {
        var request = new CreateProductRequest(
            Name: "",
            Sku: "TEST-002",
            Price: 99.99m,
            Stock: 10
        );

        var response = await _client.PostAsJsonAsync("/demo/step01/products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        error.ShouldNotBeNull();
        error.ShouldContainKey("field");
        error["field"].ShouldBe("Name");
    }

    [Fact]
    public async Task CreateProduct_NegativePrice_ShouldReturn400BadRequest()
    {
        var request = new CreateProductRequest(
            Name: "Test Product",
            Sku: "TEST-003",
            Price: -10m,
            Stock: 10
        );

        var response = await _client.PostAsJsonAsync("/demo/step01/products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        error.ShouldNotBeNull();
        error.ShouldContainKey("field");
        error["field"].ShouldBe("Price");
    }

    [Fact]
    public async Task CreateProduct_DuplicateSku_ShouldReturn409Conflict()
    {
        var request = new CreateProductRequest(
            Name: "Duplicate Product",
            Sku: "LAP-001", // Already exists in seed data
            Price: 99.99m,
            Stock: 10
        );

        var response = await _client.PostAsJsonAsync("/demo/step01/products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        error.ShouldNotBeNull();
        error.ShouldContainKey("error");
    }
}
