using System.Net;
using UnionRailway.AspNetCore.OpenApi;
using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 15 — Why can't we expose possible errors in the Swagger documentation?
///
/// We cleaned up all layers with Rail<T>. The final issue requires us to ask: how will
/// API consumers know which status codes to expect? Hand-typing [ProducesResponseType]
/// relies entirely on human memory — and will eventually fall out of sync.
///
/// ❓ Question: Can we produce automated Swagger documentation for endpoints that yield a Rail<T>?
///
/// WithRailOpenApi<TBuilder, TSuccess>() inherently injects standard error codes
/// (400/401/403/404/409/422/500) into Swagger's responses automatically. 
/// WithCreatedRailOpenApi emits a 201, and custom options can document specific codes exclusively. 
/// Now the cycle is complete: type-safe code + automatic documentation, zero repetition.
/// </summary>
public static class Step15_OpenApi
{
    public static RouteGroupBuilder MapStep15(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step15").WithTags("15 - OpenAPI Documentation");

        // ── Default: 200 + standard error codes ───────────────────────────────
        group.MapGet("/products/{id:int}", async (int id, ProductService svc) =>
            (await svc.GetByIdAsync(id)).ToHttpResult())
        .WithSummary("WithRailOpenApi — auto-generates 200/400/401/403/404/409/422/500 in Swagger")
        .WithDescription(
            "Click 'Responses' in Swagger — all standard error codes are documented automatically. " +
            "No manual [ProducesResponseType] attributes needed. Try id=1 (200), id=999 (404).")
        .WithRailOpenApi<RouteHandlerBuilder, Product>();




        // ── Created: 201 + location header ────────────────────────────────────
        group.MapPost("/products", async (CreateProductRequest req, ProductService svc) =>
        {
            var result = await svc.CreateAsync(req);
            return result.ToHttpResult(
                createdUri: result.IsSuccess(out var p, out _) ? $"/step15/products/{p.Id}" : null);
        })
        .WithSummary("WithCreatedRailOpenApi — documents 201 Created response")
        .WithDescription(
            "WithCreatedRailOpenApi adds 201 to Swagger alongside all error codes. " +
            "Try: {\"name\":\"Speaker\",\"sku\":\"SPK-007\",\"price\":79.99,\"stock\":15}")
        .WithCreatedRailOpenApi<RouteHandlerBuilder, Product>();




        // ── Custom success status code ─────────────────────────────────────────
        group.MapGet("/products/{id:int}/accepted", async (int id, ProductService svc) =>
            (await svc.GetByIdAsync(id)).ToHttpResult())
        .WithSummary("WithRailOpenApi(202) — custom success status code in docs")
        .WithDescription(
            "Pass a custom status code to document a non-standard success response (e.g. 202 Accepted). " +
            "The actual HTTP response is still governed by ToHttpResult().")
        .WithRailOpenApi<RouteHandlerBuilder, Product>((int)HttpStatusCode.Accepted); // => 202




        // ── Custom options: selective error codes ──────────────────────────────
        group.MapGet("/products/{id:int}/minimal-docs", async (int id, ProductService svc) =>
            (await svc.GetByIdAsync(id)).ToHttpResult())
        .WithSummary("WithRailOpenApi with options — pick which error codes to include")
        .WithDescription(
            "Use the options overload to include only the error codes relevant to THIS endpoint.")
        .WithRailOpenApi<RouteHandlerBuilder, Product>(opt =>
        {
            opt.IncludeValidation   = false;
            opt.IncludeUnauthorized = false;
            opt.IncludeForbidden    = false;
            // Only NotFound (404), Conflict (409), and SystemFailure (500) documented
        });

        return app;
    }
}
