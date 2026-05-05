using System.Net;
using System.Net.Http.Json;
using UnionRailway;
using UnionRailway.HttpClient;

namespace UnionRailway.Demo.Services;

/// <summary>
/// Fake HTTP handler that simulates an external product catalog API.
/// Returns 200 for known IDs, 404 for unknown, 401 for id=401, 500 for id=500.
/// </summary>
public sealed class FakeExternalHandler : DelegatingHandler
{
    private static readonly Dictionary<int, ExternalProductDto> _catalog = new()
    {
        [10] = new(10, "External Keyboard",  79.99m),
        [20] = new(20, "External Monitor",  399.99m),
    };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var idStr = path.Split('/').LastOrDefault() ?? "";

        if (!int.TryParse(idStr, out var id))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        }

        if (id == 401)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }

        if (id == 500)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }

        if (_catalog.TryGetValue(id, out var dto))
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK);
            msg.Content = JsonContent.Create(dto);
            return Task.FromResult(msg);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

/// <summary>
/// Typed client wrapping the external catalog API — returns Rail<T> via UnionRailway.HttpClient.
/// </summary>
public sealed class ExternalCatalogClient(System.Net.Http.HttpClient http)
{
    public ValueTask<Rail<ExternalProductDto>> GetProductAsync(int id, CancellationToken ct = default)
        => http.GetFromJsonAsUnionAsync<ExternalProductDto>($"/external/products/{id}", ct);
}
