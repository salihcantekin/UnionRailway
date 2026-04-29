using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 12 — I don't want to call EnsureSuccessStatusCode() when calling external APIs!
///
/// We've cleaned up EF Core. Now for external HTTP services. Currently, after every call,
/// we have response.EnsureSuccessStatusCode() and then null/exception checking.
/// Was it a 401, 404, or 500? We handle it manually inside a switch statement.
///
/// ❓ Question: What if the HTTP status code automatically translated to a typed UnionError,
///          and the returned Rail<T> could chain right into our local pipeline?
///
/// GetFromJsonAsUnionAsync: 2xx → success, 4xx/5xx → a dedicated UnionError type.
/// No EnsureSuccessStatusCode(), no try/catch, no null check anywhere.
/// Next step: Wrapping legacy code that we cannot change.
/// </summary>
public static class Step12_HttpClient
{
    public static RouteGroupBuilder MapStep12(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step12").WithTags("12 - HttpClient Integration");

        // ── 200 OK → Rail<T> success ───────────────────────────────────────────
        group.MapGet("/external/products/{id:int}", async (int id, ExternalCatalogClient client) =>
        {
            // 💡 HTTP 200 → deserialize body → Rail<ExternalProductDto> success
            //    HTTP 404 → Rail<ExternalProductDto> with UnionError.NotFound
            //    HTTP 401 → Rail<ExternalProductDto> with UnionError.Unauthorized
            //    HTTP 500 → Rail<ExternalProductDto> with UnionError.SystemFailure
            //    No HttpResponseMessage.EnsureSuccessStatusCode() anywhere!
            var result = await client.GetProductAsync(id);
            return result.ToHttpResult();
        })
        .WithSummary("GetFromJsonAsUnionAsync — HTTP status → Rail<T>")
        .WithDescription(
            "HTTP status codes map to typed errors automatically. " +
            "Try id=10 (200 OK), id=20 (200 OK), id=999 (404 NotFound), id=401 (401 Unauthorized), id=500 (500 SystemFailure).");





        // ── Chain HttpClient result into local pipeline ────────────────────────
        group.MapGet("/external/products/{id:int}/enriched", async (int id, ExternalCatalogClient extClient, DemoDbContext db) =>
        {
            // 💡 Chain external HTTP result with local logic — same railway operators
            var result = await extClient.GetProductAsync(id)
                .MapAsync(ext => new
                {
                    ext.Id,
                    ext.Name,
                    ext.Price,
                    Source   = "External Catalog",
                    FetchedAt = DateTime.UtcNow
                });

            return result.ToHttpResult();
        })
        .WithSummary("Chain HttpClient result with Map")
        .WithDescription(
            "External HTTP result flows into Map just like any Rail<T>. " +
            "Try id=10 (enriched DTO), id=999 (404 from external API).");

        return app;
    }
}
