using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 08 — Do we really have to write .ToHttpResult() on every endpoint?
///
/// In the previous step we solved the mapping issue with .ToHttpResult(). However,
/// in a project with 50 endpoints, writing .ToHttpResult() on each one is still repetition. 
/// What if we forget one?
///
/// ❓ Question: Can we define this transformation once and apply it to an entire group?
///
/// WithRailwayFilter() is appended to a route group once; then all its handlers simply return
/// Rail<T> directly, and the filter handles the translation to IResult. No .ToHttpResult() needed.
/// In the next step, we will see how to customize ProblemDetails responses.
/// </summary>
public static class Step08_RailwayFilter
{
    public static RouteGroupBuilder MapStep08(this RouteGroupBuilder app)
    {
        // 💡 ONE LINE here replaces .ToHttpResult() on EVERY endpoint in this group
        var group = app
            .MapGroup("/step08")
            .WithTags("08 - Railway Filter (Zero Boilerplate)")
            .WithRailwayFilter(); // <== Here!!!

        // Notice: handlers just return Rail<T> — no .ToHttpResult() anywhere!

        group.MapGet("/products", async (ProductService svc) => await svc.GetAllAsync())
            .WithSummary("GET all — returns Rail<List<Product>> directly")
            .WithDescription("No .ToHttpResult() needed. Filter converts Rail<T> to IResult automatically.");

        group.MapGet("/products/{id:int}", async (int id, ProductService svc) => await svc.GetByIdAsync(id))
            .WithSummary("GET by id — returns Rail<Product> directly")
            .WithDescription(
                "Returns Rail<Product> directly from handler. Filter does the conversion. " +
                "Try id=1 (200), id=999 (404 ProblemDetails).");

        group.MapPost("/products", async (CreateProductRequest req, ProductService svc) => await svc.CreateAsync(req))
            .WithSummary("POST product — returns Rail<Product> directly")
            .WithDescription(
                "Rail<Product> → 200 OK (filter doesn't know the URI here so no 201; " +
                "use ToHttpResult(createdUri:...) in Step07 for 201). " +
                "Try: {\"name\":\"Tablet\",\"sku\":\"TAB-005\",\"price\":499,\"stock\":8}");

        group.MapDelete("/products/{id:int}", async Task<Rail<Unit>> (int id, ProductService svc) =>
        {
            // Rail<Unit> → 204 No Content automatically via filter
            var getResult = await svc.GetByIdAsync(id);

            if (!getResult.IsSuccess(out var product, out var err))
            {
                return Union.Fail<Unit>(err.GetValueOrDefault());
            }

            // Simulate delete (we don't actually delete for demo purposes)
            _ = product;

            return Union.Ok(Unit.Value);
        })
        .WithSummary("DELETE — Rail<Unit> → 204 No Content")
        .WithDescription(
            "Rail<Unit> automatically returns 204 No Content. " +
            "Try id=1 (204), id=999 (404 ProblemDetails).");

        return app;
    }
}
