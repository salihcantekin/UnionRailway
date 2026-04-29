using Microsoft.EntityFrameworkCore;
using UnionRailway.EntityFrameworkCore;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 11 — Ditch the Null Check and Try/Catch maze in EF Core!
///
/// We have written services that return Rail<T>. But in the database layer, we
/// still have boilerplate null checks (FindAsync → null → NotFound) and try/catch 
/// (SaveChanges → exception). This boilerplate leaks back into the service layer.
///
/// ❓ Question: What if FirstOrDefault automatically converted a null to NotFound, 
///          and SaveChanges translated an exception into a SystemFailure behind the scenes?
///
/// FirstOrDefaultAsUnionAsync: null → UnionError.NotFound automatically.
/// SaveChangesAsUnionAsync: exception → UnionError.SystemFailure automatically.
/// There are no try/catch or null-check statements remaining in the EF Core layer.
/// In the next step, we will apply this approach to external service calls (HttpClient).
/// </summary>
public static class Step11_EfCore
{
    public static RouteGroupBuilder MapStep11(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step11").WithTags("11 - EF Core Integration");

        // ── FirstOrDefaultAsUnionAsync ─────────────────────────────────────────
        group.MapGet("/products/{id:int}", async (int id, DemoDbContext db) =>
        {
            // 💡 Without UnionRailway: FindAsync returns null → you check null → manual NotFound
            // With UnionRailway: FirstOrDefaultAsUnionAsync does this automatically
            var result = await db.Products.FirstOrDefaultAsUnionAsync("Product", p => p.Id == id);

            return result.ToHttpResult();
        })
        .WithSummary("FirstOrDefaultAsUnionAsync — null → NotFound automatically")
        .WithDescription(
            "No null checks. Null result is automatically converted to UnionError.NotFound(resource). " +
            "Try id=1 (200 OK), id=999 (404 NotFound).");








        group.MapGet("/products/by-sku/{sku}", async (string sku, DemoDbContext db) =>
        {
            var result = await db.Products.FirstOrDefaultAsUnionAsync("Product", p => p.Sku == sku);
             
            return result.ToHttpResult();
        })
        .WithSummary("FirstOrDefaultAsUnionAsync with string predicate")
        .WithDescription("Try sku=LAP-001 (200), sku=UNKNOWN (404 with resource='Product').");




        // ── SaveChangesAsUnionAsync ────────────────────────────────────────────
        group.MapPost("/products", async Task<IResult> (CreateProductRequest req, DemoDbContext db) =>
        {
            // 💡 SaveChangesAsUnionAsync: DbUpdateException / concurrency → SystemFailure
            //    No try/catch around SaveChangesAsync ever again
            var conflicts = await db.Products.AnyAsync(p => p.Sku == req.Sku);
            if (conflicts)
            {
                return Union.Fail<Product>(new UnionError.Conflict($"SKU '{req.Sku}' already exists")).ToHttpResult();
            }

            var product = new Product { Name = req.Name, Sku = req.Sku, Price = req.Price, Stock = req.Stock };
            db.Products.Add(product);

            // 💡 Returns Rail<int> — the number of rows saved, or SystemFailure on exception
            var saved = await db.SaveChangesAsUnionAsync();
            if (!saved.IsSuccess(out _, out var err))
            {
                return err.GetValueOrDefault().ToHttpResult();
            }

            return Results.Created($"/step11/products/{product.Id}", product);
        })
        .WithSummary("SaveChangesAsUnionAsync — DbUpdateException → SystemFailure")
        .WithDescription(
            "No try/catch around SaveChanges. Exceptions map to UnionError.SystemFailure automatically. " +
            "Try: {\"name\":\"New Item\",\"sku\":\"NEW-006\",\"price\":19.99,\"stock\":10} " +
            "Then repeat the same SKU to see the Conflict.");

        return app;
    }
}
