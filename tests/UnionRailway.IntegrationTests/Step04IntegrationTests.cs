using System.Net;
using System.Net.Http.Json;
using Shouldly;
using UnionRailway.Demo;
using UnionRailway.Demo.Services;
using Xunit;

namespace UnionRailway.IntegrationTests;

public class Step04IntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly System.Net.Http.HttpClient _client;

    public Step04IntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Style1_IsSuccess_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step04/style1-issuccess/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        result.ShouldNotBeNull();
        result.ShouldContainKey("style");
        result["style"].ToString().ShouldBe("IsSuccess");
    }

    [Fact]
    public async Task Style1_IsSuccess_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step04/style1-issuccess/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task Style2_Match_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step04/style2-match/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        result.ShouldNotBeNull();
        result.ShouldContainKey("style");
        result["style"].ToString().ShouldBe("Match");
    }

    [Fact]
    public async Task Style2_Match_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step04/style2-match/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task Style3_ErrorProperty_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step04/style3-error-prop/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        result.ShouldNotBeNull();
        result.ShouldContainKey("style");
        result["style"].ToString().ShouldBe("ErrorProperty");
    }

    [Fact]
    public async Task Style3_ErrorProperty_NonExistingProduct_ShouldReturn404ProblemDetails()
    {
        var response = await _client.GetAsync("/demo/step04/style3-error-prop/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Status.ShouldBe(404);
    }

    [Fact]
    public async Task SwitchOnError_ExistingProduct_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/demo/step04/switch-on-error/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
    }

    [Fact]
    public async Task SwitchOnError_NonExistingProduct_ShouldReturn400BadRequest()
    {
        var response = await _client.GetAsync("/demo/step04/switch-on-error/999");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problemDetails.ShouldNotBeNull();
        problemDetails.Detail.ShouldContain("Resource missing");
    }
}
