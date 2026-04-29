using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 14 — Delete/Update Operations don't return a value, what should the type be?
///
/// Up to now, we've continually returned meaningful values like Rail<Product> or Rail<Order>.
/// But when an operation such as a DELETE or deduct-stock succeeds, there isn't really a
/// return value — knowing it "succeeded" is usually enough. Could we use void? No, you
/// can't wrap void inside an envelope type of Rail.
///
/// ❓ Question: How can we type-safely express "The operation succeeded, but yields no value"?
///
/// Rail<Unit> conveys exactly this meaning. Unit.Value is a singleton with zero allocations.
/// ToHttpResult() or the RailwayFilter converts it automatically to a 204 No Content response.
/// Next step: Automated Swagger documentation generation.
/// </summary>
public static class Step14_UnitNoContent
{
    public static RouteGroupBuilder MapStep14(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step14").WithTags("14 - Rail<Unit> → 204 No Content");

        // ── Deduct stock: success = nothing to return ─────────────────────────
        group.MapPost("/products/{id:int}/deduct-stock", async (int id, int qty, ProductService svc) =>
        {
            // 💡 Rail<Unit> = "it worked, but there's nothing to return"
            //    ToHttpResult() on a successful Rail<Unit> → 204 No Content
            Rail<Unit> result = await svc.DeductStockAsync(id, qty);
            return result.ToHttpResult();
        })
        .WithSummary("Rail<Unit> → 204 No Content on success")
        .WithDescription(
            "DeductStockAsync returns Rail<Unit>. Success → 204. Error → RFC 7807 Problem. " +
            "Try id=1 qty=2 (204), id=3 qty=5 (409 Conflict — out of stock), id=999 qty=1 (404 NotFound).");







        // ── Delete pattern: find first, then delete ────────────────────────────
        group.MapDelete("/products/{id:int}", async (int id, DemoDbContext db) =>
        {
            // 💡 Pattern: validate → act → return Unit
            var product = await db.Products.FindAsync(id);
            if (product is null)
            {
                return Union.Fail<Unit>(new UnionError.NotFound("Product")).ToHttpResult();
            }

            db.Products.Remove(product);
            await db.SaveChangesAsync();

            // Unit.Value is the singleton — no allocation
            return Union.Ok(Unit.Value).ToHttpResult();
        })
        .WithSummary("DELETE endpoint returning Rail<Unit>")
        .WithDescription(
            "Delete returns Rail<Unit>. Success → 204. Not found → 404. " +
            "Try id=2 (deletes mouse → 204), id=999 (404).");











        // ── With Railway Filter: Rail<Unit> → 204 automatically ───────────────
        var filtered = app
            .MapGroup("/step14/filtered")
            .WithTags("14 - Rail<Unit> → 204 No Content")
            .WithRailwayFilter();


            // 💡 With filter: just return Rail<Unit> — no .ToHttpResult() needed
        filtered.MapPost("/products/{id:int}/deduct-stock", async (int id, int qty, ProductService svc) => await svc.DeductStockAsync(id, qty))
        .WithSummary("Rail<Unit> + RailwayFilter → 204 (zero boilerplate)")
        .WithDescription(
            "WithRailwayFilter() handles Rail<Unit> → 204 automatically. " +
            "Try id=1 qty=1 (204), id=3 qty=1 (409 Conflict).");

        return app;
    }
}
