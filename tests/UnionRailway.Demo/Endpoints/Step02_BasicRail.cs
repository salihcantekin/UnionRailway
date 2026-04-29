using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 02 — A Clean Start with Rail<T>
///
/// In the previous step, each endpoint returned a different error shape and the caller did not
/// know what to expect. Now we write the same scenarios with Rail<T>.
///
/// ❓ Question: Can you look at a method signature and say "this might fail"?
///          ValueTask<Rail<Product>> tells you exactly that — no need for try/catch.
///
/// Rail<T> carries either a success value or a UnionError. Never both at the same time.
/// In the next step, we will look exactly at what is inside this UnionError.
/// </summary>
public static class Step02_BasicRail
{
    public static RouteGroupBuilder MapStep02(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step02").WithTags("02 - Basic Rail<T>");

        // ── Get product: Rail<T> with IsSuccess pattern ───────────────────────
        group.MapGet("/products/{id:int}", async Task<IResult> (int id, ProductService svc) =>
        {
            // 💡 The return type Rail<Product> TELLS YOU it can fail — no surprises.
            Rail<Product> result = await svc.GetByIdAsync(id);

            // IsSuccess deconstructs into success value AND error in one call
            if (!result.IsSuccess(out var product, out var error))
            {
                return error.GetValueOrDefault().ToHttpResult();
            }

            return Results.Ok(product);
        })
        .WithSummary("GET product — Rail<T> with IsSuccess pattern")
        .WithDescription(
            "✅ AFTER: Rail<Product> is the contract. IsSuccess deconstructs both slots at once. " +
            "Try id=1 (200 OK), id=999 (404 Problem Details).");





        // ── Get all: success is just a list — implicit conversion ─────────────
        group.MapGet("/products", async (ProductService svc) =>
        {
            // 💡 Implicit conversion: List<Product> → Rail<List<Product>> automatically
            Rail<List<Product>> result = await svc.GetAllAsync();

            return result.IsSuccess(out var products, out _)
                ? Results.Ok(products)
                : Results.Problem("Failed to load products");
        })
        .WithSummary("GET all products — implicit Rail<T> conversion")
        .WithDescription(
            "✅ A plain List<T> implicitly converts to Rail<List<T>> — no boilerplate factory needed.");






        // ── Explicit Union.Ok / Union.Fail ────────────────────────────────────
        group.MapGet("/products/{id:int}/explicit", async Task<IResult> (int id, DemoDbContext db) =>
        {
            // 💡 You can also create rails explicitly with Union.Ok / Union.Fail
            var product = await db.Products.FindAsync(id);

            Rail<Product> result = product is null
                ? Union.Fail<Product>(new UnionError.NotFound("Product"))
                : Union.Ok(product);
            
            return result.IsSuccess(out var p, out var err)
                ? Results.Ok(p)
                : err.GetValueOrDefault().ToHttpResult();
        })
        .WithSummary("GET product — explicit Union.Ok / Union.Fail construction")
        .WithDescription(
            "✅ Union.Ok(value) and Union.Fail<T>(error) for explicit rail construction. " +
            "Try id=1 (200), id=999 (404).");



        return app;
    }
}
