using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 05 — Chaining: Do we need if/else at every step?
///
/// In the previous step, we handled a single Rail<T>. In real life, we perform
/// multiple operations successively: fetch the product, check stock, convert to DTO.
/// Writing `if (!result.IsSuccess(...)) return` at every step quickly becomes tedious.
///
/// ❓ Question: What should the chain do if any of those steps fail?
///          It shouldn't run the rest — the error should automatically propagate.
///
/// Map (transforms value), Bind (chains an operation that returns a Rail) and their
/// async counterparts provide this. The moment an error occurs, they short-circuit,
/// and subsequent steps are skipped. Next step: side effects (logging) and fallbacks.
/// </summary>
public static class Step05_Composition
{
    public static RouteGroupBuilder MapStep05(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step05").WithTags("05 - Railway Composition");

        // ── Map: transform success value synchronously ─────────────────────────
        group.MapGet("/map/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 Map transforms the success value. On error it short-circuits — no if/else needed.
            var result = (await svc.GetByIdAsync(id))
                .Map(p => new
                {
                    p.Id,
                    p.Name,
                    PriceWithTax = p.Price * 1.2m
                });

            return result.ToHttpResult();
        })
        .WithSummary("Map — transform success value")
        .WithDescription(
            "Map(fn) transforms the inner value on success; passes error through unchanged. " +
            "Try id=1 (price * 1.2), id=999 (error short-circuits).");







        // ── Bind: chain operations that themselves return Rail<T> ─────────────
        group.MapGet("/bind/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 Bind chains a function that also returns Rail<T>.
            // If GetByIdAsync fails → Bind never runs → error propagates.
            // Using BindObject with predicate style for cleaner code with object results.
            var result = (await svc.GetByIdAsync(id))
                .BindObject(
                    p => p.Stock > 0,
                    p => new { p.Id, p.Name, Status = "In Stock" },
                    p => new UnionError.Conflict("Out of stock"));

            return result.ToHttpResult();
        })
        .WithSummary("Bind — chain fallible operations")
        .WithDescription(
            "Bind(fn) chains a function returning Rail<T>. Error short-circuits the whole chain. " +
            "Try id=1 (in stock), id=3 (stock=0 → Conflict), id=999 (NotFound).");







        // ── MapAsync + BindAsync chain ─────────────────────────────────────────
        group.MapGet("/chain/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 Chain multiple async operations — reads like a happy-path story.
            // Any step returning an error stops the chain immediately.
            var result = await svc.GetByIdAsync(id)
                .BindAsync(p => svc.GetBySkuAsync(p.Sku))          // re-fetch by SKU
                .MapAsync(p => new { p.Id, p.Name, p.Sku, p.Stock })
                .ToHttpResultAsync();

            return result;
        })
        .WithSummary("MapAsync + BindAsync chain — async pipeline")
        .WithDescription(
            "Full async railway chain: GetById → GetBySku → Map to DTO → ToHttpResult. " +
            "Error at ANY step halts the chain. Try id=1 (full chain), id=999 (stops at step 1).");









        // ── Map then ToHttpResult in one line ─────────────────────────────────
        group.MapGet("/oneliner/{id:int}", async (int id, ProductService svc) =>
        {
            var result = await svc.GetByIdAsync(id);

            return result
                .Map(p => new { p.Name, FormattedPrice = $"${p.Price:F2}" })
                .ToHttpResult();
        })
        .WithSummary("One-liner: Map + ToHttpResult")
        .WithDescription("The entire endpoint fits in one expression. Clean, readable, type-safe.");

        return app;
    }
}
