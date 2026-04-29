using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 07 — How do we bridge Rail<T> to HTTP?
///
/// We return Rail<T> from the service layer. But at the endpoint level, we still
/// have to return an IResult. Should we manually pick 404/409/422 at every endpoint
/// depending on the error type? This would take us right back to the chaos of Step 01.
///
/// ❓ Question: Can we write the Rail<T> → IResult transformation once and use it everywhere?
///
/// ToHttpResult() does exactly that. On success, it yields 200 OK (or 201 Created),
/// and on error, it yields an automatic RFC 7807 ProblemDetails according to the error type.
/// One line, everywhere. In the next step, we'll eliminate even this single line.
/// </summary>
public static class Step07_AspNetCore
{
    public static RouteGroupBuilder MapStep07(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step07").WithTags("07 - ASP.NET Core Integration");

        // ── Basic ToHttpResult ─────────────────────────────────────────────────
        group.MapGet("/products/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 One line. That's it. Success → 200 OK, error → RFC 7807 Problem Details.
            (await svc.GetByIdAsync(id)).ToHttpResult();
        })
        .WithSummary("ToHttpResult — one-line HTTP bridge")
        .WithDescription(
            "✅ Success → 200 OK with JSON body. Error → RFC 7807 ProblemDetails. " +
            "Try id=1 (200), id=999 (404 ProblemDetails).");






        // ── 201 Created with location header ──────────────────────────────────
        group.MapPost("/products", async (CreateProductRequest req, ProductService svc) =>
        {
            var result = await svc.CreateAsync(req);

            // 💡 Pass createdUri → automatically returns 201 Created + Location header
            return result.ToHttpResult(createdUri: result.IsSuccess(out var p, out _)
                ? $"/step07/products/{p.Id}"
                : null);
        })
        .WithSummary("201 Created with createdUri parameter")
        .WithDescription(
            "Pass createdUri to ToHttpResult for automatic 201 Created + Location header. " +
            "Try: {\"name\":\"Headphones\",\"sku\":\"HEAD-004\",\"price\":99.99,\"stock\":20}");




        // ── ToHttpResultAsync (awaitable extension) ────────────────────────────
        group.MapGet("/products/{id:int}/async", async (int id, ProductService svc) =>
        {
            // 💡 ToHttpResultAsync awaits the ValueTask directly — no intermediate variable
            await svc.GetByIdAsync(id).ToHttpResultAsync();
        })
        .WithSummary("ToHttpResultAsync — awaitable extension on ValueTask<Rail<T>>")
        .WithDescription("Awaits ValueTask<Rail<T>> and converts to IResult in one call.");

        return app;
    }
}
