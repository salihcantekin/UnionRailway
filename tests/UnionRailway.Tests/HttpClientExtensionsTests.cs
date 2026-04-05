using System.Net;
using System.Text.Json;
using UnionRailway;
using UnionRailway.HttpClient;

namespace UnionRailway.Tests;

public sealed class HttpClientExtensionsTests
{
    // ── Helpers ─────────────────────────────────────────────────────

    private static System.Net.Http.HttpClient MakeClient(
        HttpStatusCode status,
        string? body = null,
        string contentType = "application/json")
    {
        var handler = new FakeHandler(status, body, contentType);
        return new System.Net.Http.HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
    }

    private sealed class FakeHandler(
        HttpStatusCode status,
        string? body,
        string contentType) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status);
            if (body is not null)
                response.Content = new StringContent(
                    body, System.Text.Encoding.UTF8, contentType);
            return Task.FromResult(response);
        }
    }

    private record PersonDto(string Name, int Age);

    // ── 200 OK ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetFromJsonAsUnionAsync_200_ReturnsOk()
    {
        var payload = JsonSerializer.Serialize(new { Name = "Alice", Age = 30 });
        var client  = MakeClient(HttpStatusCode.OK, payload);

        var result = await client.GetFromJsonAsUnionAsync<PersonDto>("/people/1");

        Assert.Null(result.Error);
        Assert.Equal("Alice", result.Unwrap().Name);
    }

    [Fact]
    public async Task GetFromJsonAsUnionAsync_201_ReturnsOk()
    {
        var payload = JsonSerializer.Serialize(new { Name = "Bob", Age = 25 });
        var client  = MakeClient(HttpStatusCode.Created, payload);

        var result = await client.GetFromJsonAsUnionAsync<PersonDto>("/people");

        Assert.Null(result.Error);
        Assert.Equal("Bob", result.Unwrap().Name);
    }

    // ── 400 Validation ───────────────────────────────────────────────

    [Fact]
    public async Task GetFromJsonAsUnionAsync_400WithProblemDetails_ReturnsValidation()
    {
        var problemBody = JsonSerializer.Serialize(new
        {
            title  = "One or more validation errors occurred.",
            errors = new { Email = new[] { "Invalid format" }, Name = new[] { "Required" } }
        });
        var client = MakeClient(HttpStatusCode.BadRequest, problemBody, "application/problem+json");

        var result = await client.GetFromJsonAsUnionAsync<PersonDto>("/people");

        Assert.NotNull(result.Error);
        var v = Assert.IsType<UnionError.Validation>(result.Error);
        Assert.Contains("Email", v.Fields.Keys);
        Assert.Contains("Name",  v.Fields.Keys);
    }

    // ── 404 NotFound ─────────────────────────────────────────────────

    [Fact]
    public async Task GetFromJsonAsUnionAsync_404_ReturnsNotFound()
    {
        var client = MakeClient(HttpStatusCode.NotFound);

        var result = await client.GetFromJsonAsUnionAsync<PersonDto>("/people/999");

        Assert.NotNull(result.Error);
        Assert.IsType<UnionError.NotFound>(result.Error);
    }

    // ── 401 Unauthorized ────────────────────────────────────────────

    [Fact]
    public async Task GetFromJsonAsUnionAsync_401_ReturnsUnauthorized()
    {
        var client = MakeClient(HttpStatusCode.Unauthorized);

        var result = await client.GetFromJsonAsUnionAsync<PersonDto>("/secure");

        Assert.NotNull(result.Error);
        Assert.IsType<UnionError.Unauthorized>(result.Error);
    }

    // ── DELETE helper ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsUnionAsync_200_ReturnsTrue()
    {
        var client = MakeClient(HttpStatusCode.OK);

        var result = await client.DeleteAsUnionAsync("/items/1");

        Assert.Null(result.Error);
        Assert.True(result.Value);
    }
}
