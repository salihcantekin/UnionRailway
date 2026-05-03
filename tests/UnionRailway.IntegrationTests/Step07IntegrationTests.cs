using System.Net;
using System.Net.Http.Json;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using UnionRailway.Demo;
using UnionRailway.Demo.Services;
using Xunit;

namespace UnionRailway.IntegrationTests;

public class Step07IntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly System.Net.Http.HttpClient _client;

    public Step07IntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProduct_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step07/products/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetProduct_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step07/products/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturn201CreatedAndLocationHeader()
    {
        var faker = new Faker();
        var createRequest = new CreateProductRequest(
            Name: faker.Commerce.ProductName(),
            Sku: faker.Commerce.Ean13(),
            Price: decimal.Parse(faker.Commerce.Price()),
            Stock: faker.Random.Int(1, 100)
        );

        var response = await _client.PostAsJsonAsync("/demo/step07/products", createRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldMatch(@"/step07/products/\d+");

        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Name.ShouldBe(createRequest.Name);
        product.Sku.ShouldBe(createRequest.Sku);
    }

    [Fact]
    public async Task GetProductAsync_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step07/products/1/async");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetProductAsync_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step07/products/999/async");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }
}
