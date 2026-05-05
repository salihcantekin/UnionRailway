using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 13 — What to do with legacy code I can't rewrite?
///
/// We have migrated EF Core and HttpClient to Rail<T>. But there are old services
/// in the project that we can't touch right now. They hurl exceptions or return null.
/// Must we write separate try/catch wrappers all around our codebase?
///
/// ❓ Question: Can we bridge a legacy service that throws exceptions into the Rail<T>
///          world without altering its source code?
///
/// UnionWrapper.RunAsync automatically translates: KeyNotFoundException → NotFound,
/// UnauthorizedAccessException → Unauthorized, everything else → SystemFailure. 
/// One line, entirely seamless from legacy. RunNullableAsync does the same for null-returners. 
/// Next step: Handling operations that don't return values.
/// </summary>
public static class Step13_UnionWrapper
{
    public static RouteGroupBuilder MapStep13(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step13").WithTags("13 - UnionWrapper (Legacy Migration)");

        // ── Wrap a legacy service that throws ─────────────────────────────────
        group.MapGet("/legacy/products/{id:int}", async (int id, LegacyInventoryService legacy) =>
        {
            // 💡 LegacyInventoryService.GetProductAsync throws:
            //    KeyNotFoundException for id=999
            //    UnauthorizedAccessException for id=401
            //    Returns normally for everything else
            //
            // UnionWrapper.RunAsync catches all of that and returns Rail<T>
            var result = await UnionWrapper.RunAsync(() => legacy.GetProductAsync(id));

            return result.ToHttpResult();
        })
        .WithSummary("UnionWrapper.RunAsync — wrap throwing legacy service")
        .WithDescription(
            "Legacy code throws; UnionWrapper catches and maps to typed errors. " +
            "Try id=1 (200 OK), id=999 (KeyNotFoundException → 404 NotFound), id=401 (UnauthorizedAccessException → 401).");






        // ── RunNullableAsync: wrap nullable-returning legacy code ─────────────
        group.MapGet("/legacy/nullable/{id:int}", async (int id) =>
        {
            // 💡 RunNullableAsync: null return → UnionError.NotFound automatically
            var result = await UnionWrapper.RunNullableAsync(async () =>
            {
                await Task.Delay(1); // simulate async
                return id == 999
                        ? null
                        : new Product
                          {
                            Id = id,
                            Name = "Wrapped Product",
                            Sku = $"WRP-{id:000}",
                            Price = 19.99m,
                            Stock = 5
                        };
            });

            return result.ToHttpResult();
        })
        .WithSummary("UnionWrapper.RunNullableAsync — null return → NotFound")
        .WithDescription(
            "Wraps a method that returns null on 'not found'. " +
            "Null is converted to UnionError.NotFound automatically. " +
            "Try id=1 (200), id=999 (null → 404).");










        // ── Show the before/after side-by-side via descriptions ───────────────
        group.MapGet("/legacy/before-example/{id:int}", async (int id, LegacyInventoryService legacy) =>
        {
            // ❌ BEFORE (old migration approach): manual try/catch everywhere
            try
            {
                var product = await legacy.GetProductAsync(id);
                return Results.Ok(product);
            }
            catch (KeyNotFoundException ex)      { return Results.NotFound(new { error = ex.Message }); }
            catch (UnauthorizedAccessException)  { return Results.Unauthorized(); }
            catch (Exception ex)                 { return Results.Problem(ex.Message); }
        })
        .WithSummary("BEFORE: manual try/catch for legacy code")
        .WithDescription("The old approach — try/catch everywhere. Compare with UnionWrapper endpoint above.");

        return app;
    }
}
