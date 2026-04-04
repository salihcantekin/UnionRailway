using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using UnionRailway.HttpClient;

namespace UnionRailway.Tests.HttpClient;

public record WeatherResponse(string City, double Temperature);

public class HttpClientExtensionsTests : IDisposable
{
    private readonly System.Net.Http.HttpClient client;
    private readonly MockHttpMessageHandler handler;

    public HttpClientExtensionsTests()
    {
        this.handler = new MockHttpMessageHandler();
        this.client = new System.Net.Http.HttpClient(this.handler)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnOk_WhenStatusIs200()
    {
        this.handler.ResponseFactory = _ => CreateJsonResponse(
            HttpStatusCode.OK,
            new WeatherResponse("Istanbul", 25.0));

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather");

        result.Should().BeOfType<Result<WeatherResponse>.Ok>();
        var ok = (Result<WeatherResponse>.Ok)result;
        ok.Data.City.Should().Be("Istanbul");
        ok.Data.Temperature.Should().Be(25.0);
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnNotFound_WhenStatusIs404()
    {
        this.handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather/unknown");

        result.Should().BeOfType<Result<WeatherResponse>.Error>();
        var error = (Result<WeatherResponse>.Error)result;
        error.Err.Should().BeOfType<UnionError.NotFound>();
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnUnauthorized_WhenStatusIs401()
    {
        this.handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather");

        result.Should().BeOfType<Result<WeatherResponse>.Error>();
        var error = (Result<WeatherResponse>.Error)result;
        error.Err.Should().BeOfType<UnionError.Unauthorized>();
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnUnauthorized_WhenStatusIs403()
    {
        this.handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.Forbidden);

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather");

        result.Should().BeOfType<Result<WeatherResponse>.Error>();
        var error = (Result<WeatherResponse>.Error)result;
        error.Err.Should().BeOfType<UnionError.Unauthorized>();
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnConflict_WhenStatusIs409()
    {
        this.handler.ResponseFactory = _ => CreateStringResponse(HttpStatusCode.Conflict, "Resource already exists");

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather");

        result.Should().BeOfType<Result<WeatherResponse>.Error>();
        var error = (Result<WeatherResponse>.Error)result;
        error.Err.Should().BeOfType<UnionError.Conflict>();
        ((UnionError.Conflict)error.Err).Reason.Should().Be("Resource already exists");
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnValidation_WhenStatusIs400WithFields()
    {
        var validationErrors = new Dictionary<string, string>
        {
            ["Name"] = "Name is required",
            ["Age"] = "Age must be positive"
        };
        this.handler.ResponseFactory = _ => CreateJsonResponse(HttpStatusCode.BadRequest, validationErrors);

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather");

        result.Should().BeOfType<Result<WeatherResponse>.Error>();
        var error = (Result<WeatherResponse>.Error)result;
        error.Err.Should().BeOfType<UnionError.Validation>();
        var validation = (UnionError.Validation)error.Err;
        validation.Fields.Should().HaveCount(2);
        validation.Fields["Name"].Should().Be("Name is required");
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnValidation_WhenStatusIs400WithPlainText()
    {
        this.handler.ResponseFactory = _ => CreateStringResponse(HttpStatusCode.BadRequest, "Invalid input");

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather");

        result.Should().BeOfType<Result<WeatherResponse>.Error>();
        var error = (Result<WeatherResponse>.Error)result;
        error.Err.Should().BeOfType<UnionError.Validation>();
        var validation = (UnionError.Validation)error.Err;
        validation.Fields.Should().ContainKey("General");
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnSystemFailure_WhenStatusIs500()
    {
        this.handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather");

        result.Should().BeOfType<Result<WeatherResponse>.Error>();
        var error = (Result<WeatherResponse>.Error)result;
        error.Err.Should().BeOfType<UnionError.SystemFailure>();
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnSystemFailure_WhenExceptionThrown()
    {
        this.handler.ResponseFactory = _ => throw new HttpRequestException("Network error");

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather");

        result.Should().BeOfType<Result<WeatherResponse>.Error>();
        var error = (Result<WeatherResponse>.Error)result;
        error.Err.Should().BeOfType<UnionError.SystemFailure>();
        ((UnionError.SystemFailure)error.Err).Ex.Message.Should().Be("Network error");
    }

    [Fact]
    public async Task PostAsUnionAsync_ShouldReturnOk_WhenStatusIs201()
    {
        this.handler.ResponseFactory = _ => CreateJsonResponse(
            HttpStatusCode.Created,
            new WeatherResponse("Ankara", 18.0));

        var content = new StringContent(
            JsonSerializer.Serialize(new { City = "Ankara" }),
            Encoding.UTF8,
            "application/json");

        var result = await this.client.PostAsUnionAsync<WeatherResponse>("weather", content);

        result.Should().BeOfType<Result<WeatherResponse>.Ok>();
        var ok = (Result<WeatherResponse>.Ok)result;
        ok.Data.City.Should().Be("Ankara");
    }

    [Fact]
    public async Task PutAsUnionAsync_ShouldReturnOk_WhenStatusIs200()
    {
        this.handler.ResponseFactory = _ => CreateJsonResponse(
            HttpStatusCode.OK,
            new WeatherResponse("Izmir", 30.0));

        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var result = await this.client.PutAsUnionAsync<WeatherResponse>("weather/1", content);

        result.Should().BeOfType<Result<WeatherResponse>.Ok>();
    }

    [Fact]
    public async Task DeleteAsUnionAsync_ShouldReturnOk_WhenStatusIsNoContent()
    {
        this.handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        var result = await this.client.DeleteAsUnionAsync<string?>("weather/1");

        result.Should().BeOfType<Result<string?>.Ok>();
    }

    [Fact]
    public async Task DeleteAsUnionAsync_ShouldReturnNotFound_WhenStatusIs404()
    {
        this.handler.ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await this.client.DeleteAsUnionAsync<string?>("weather/999");

        result.Should().BeOfType<Result<string?>.Error>();
        var error = (Result<string?>.Error)result;
        error.Err.Should().BeOfType<UnionError.NotFound>();
    }

    [Fact]
    public async Task GetAsUnionAsync_ShouldReturnOk_WhenStatusIsAccepted()
    {
        this.handler.ResponseFactory = _ => CreateJsonResponse(
            HttpStatusCode.Accepted,
            new WeatherResponse("Async City", 0));

        var result = await this.client.GetAsUnionAsync<WeatherResponse>("weather/process");

        result.Should().BeOfType<Result<WeatherResponse>.Ok>();
    }

    private static HttpResponseMessage CreateJsonResponse<T>(HttpStatusCode statusCode, T body)
    {
        var json = JsonSerializer.Serialize(body);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateStringResponse(HttpStatusCode statusCode, string body) =>
        new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain")
        };

    public void Dispose()
    {
        this.client.Dispose();
        this.handler.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// A test HTTP message handler that allows configuring response behavior per test.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = ResponseFactory(request);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
