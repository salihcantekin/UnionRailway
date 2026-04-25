using System.Net;
using System.Text.Json;
using UnionRailway;
using UnionRailway.HttpClient;

namespace UnionRailway.Tests;

public sealed class HttpClientExtensionsTests
{
    private static TError AssertError<T, TError>(Rail<T> result)
        where TError : class
    {
        Assert.True(result.TryGetError(out UnionError? error));
        return Assert.IsType<TError>(error.GetValueOrDefault().Value);
    }

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
        System.Net.Http.HttpClient client  = MakeClient(HttpStatusCode.OK, payload);

        Rail<PersonDto> result = await client.GetFromJsonAsUnionAsync<PersonDto>("/people/1");

        Assert.Equal("Alice", result.Unwrap().Name);
    }

    [Fact]
    public async Task GetFromJsonAsUnionAsync_201_ReturnsOk()
    {
        var payload = JsonSerializer.Serialize(new { Name = "Bob", Age = 25 });
        System.Net.Http.HttpClient client  = MakeClient(HttpStatusCode.Created, payload);

        Rail<PersonDto> result = await client.GetFromJsonAsUnionAsync<PersonDto>("/people");

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
        System.Net.Http.HttpClient client = MakeClient(HttpStatusCode.BadRequest, problemBody, "application/problem+json");

        Rail<PersonDto> result = await client.GetFromJsonAsUnionAsync<PersonDto>("/people");

        UnionError.Validation v = AssertError<PersonDto, UnionError.Validation>(result);
        Assert.Contains("Email", v.Fields.Keys);
        Assert.Contains("Name",  v.Fields.Keys);
    }

    // ── 404 NotFound ─────────────────────────────────────────────────

    [Fact]
    public async Task GetFromJsonAsUnionAsync_404_ReturnsNotFound()
    {
        System.Net.Http.HttpClient client = MakeClient(HttpStatusCode.NotFound);

        Rail<PersonDto> result = await client.GetFromJsonAsUnionAsync<PersonDto>("/people/999");

        AssertError<PersonDto, UnionError.NotFound>(result);
    }

    // ── 401 Unauthorized ────────────────────────────────────────────

    [Fact]
    public async Task GetFromJsonAsUnionAsync_401_ReturnsUnauthorized()
    {
        System.Net.Http.HttpClient client = MakeClient(HttpStatusCode.Unauthorized);

        Rail<PersonDto> result = await client.GetFromJsonAsUnionAsync<PersonDto>("/secure");

        AssertError<PersonDto, UnionError.Unauthorized>(result);
    }

    // ── DELETE helper ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsUnionAsync_200_ReturnsTrue()
    {
        System.Net.Http.HttpClient client = MakeClient(HttpStatusCode.OK);

        Rail<Unit> result = await client.DeleteAsUnionAsync("/items/1");

        Assert.Equal(Unit.Value, result.Unwrap());
    }
}
