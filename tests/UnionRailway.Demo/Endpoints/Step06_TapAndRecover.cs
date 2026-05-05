using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 06 — I want to log, but I don't want to alter the value!
///
/// We established a chain in the previous step. Now there's a new requirement: 
/// log on every successful result, but without breaking the Rail<T> chain. Or
/// gracefully fall back to a default value instead of throwing an exception on error.
///
/// ❓ Question: If I use Map to log in the middle of a chain, I'm forced to alter
///          the return type. How can we do this gracefully?
///
/// Tap: lets the success value pass through unchanged, solely executing a side effect.
/// Recover: intercepts a specific error type to supply a fallback value; 
/// other error types remain unaffected. Next step: bridging Rail<T> to HTTP.
/// </summary>
public static class Step06_TapAndRecover
{
    public static RouteGroupBuilder MapStep06(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step06").WithTags("06 - Tap & Recover");

        // ── Tap: log / audit without breaking the chain ───────────────────────
        group.MapGet("/tap/{id:int}", async (int id, ProductService svc, [FromServices] ILogger<Program> logger) =>
        {
            // 💡 TapAsync executes a side-effect on success, then passes the value through unchanged.
            // Perfect for logging, metrics, cache-warming — without polluting your logic.
            var result = await svc.GetByIdAsync(id)
                .TapAsync(p =>
                {
                    logger.LogInformation("Product {Id} accessed: {Name}", p.Id, p.Name);
                    return ValueTask.CompletedTask;
                })
                .ToHttpResultAsync();

            return result;
        })
        .WithSummary("TapAsync — side-effect without changing value")
        .WithDescription(
            "TapAsync(fn) calls fn on success, passes value unchanged, skips on error. " +
            "Check server logs after calling with id=1. Error path: id=999.");







        // ── Recover: typed fallback for a specific error ───────────────────────
        group.MapGet("/recover/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 RecoverAsync only fires when the error is EXACTLY UnionError.NotFound.
            // Other errors pass through untouched.
            var guestProduct = new Product { Id = 0, Name = "Guest Placeholder", Sku = "N/A", Price = 0 };

            var result = await svc.GetByIdAsync(id)
                .RecoverAsync<Product, UnionError.NotFound>(_ => guestProduct);

            return result.ToHttpResult();
        })
        .WithSummary("RecoverAsync — typed fallback on specific error")
        .WithDescription(
            "RecoverAsync<T, TError>(fn) provides a fallback ONLY for the specified error type. " +
            "Try id=999 (NotFound → returns guest placeholder instead of 404). " +
            "Other error types still propagate.");









        // ── Tap + Recover chain ────────────────────────────────────────────────
        group.MapGet("/tap-and-recover/{id:int}", async (int id, ProductService svc, [FromServices] ILogger<Program> logger) =>
        {
            var fallback = new Product { Id = -1, Name = "Default Product", Sku = "DEFAULT", Price = 0 };

            // 💡 Chain: try to get product → log access → recover if not found → respond
            var result = await svc.GetByIdAsync(id)
                .TapAsync(p =>
                {
                    logger.LogInformation("Serving real product {Id}", p.Id);
                    return ValueTask.CompletedTask;
                })
                .RecoverAsync<Product, UnionError.NotFound>(_ =>
                {
                    logger.LogWarning("Product {Id} not found — using fallback", id);
                    return fallback;
                });

            return result.ToHttpResult();
        })
        .WithSummary("Tap + Recover chain")
        .WithDescription("Full chain: real product → tap log → recover with fallback if missing. Try id=1 or id=999.");



        return app;
    }
}
