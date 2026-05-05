using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 16 — New operators: Ensure, Switch, ToFail, and SystemFailure(string)
///
/// This step introduces four convenience features:
///
/// • **Ensure** — validates a success value against a predicate and short-circuits
///   to an error when the predicate fails (prevents null-success anti-pattern).
/// • **Switch** — void counterpart of Match; executes side effects on success or
///   error without requiring a return value.
/// • **ToFail** — shorthand for creating a failed Rail from a UnionError.
/// • **SystemFailure(string)** — message-based constructor; no explicit Exception required.
/// </summary>
public static class Step16_NewOperators
{
    public static RouteGroupBuilder MapStep16(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step16").WithTags("16 - Ensure, Switch, ToFail & SystemFailure");

        // ── Ensure: guard against invalid/null success values ─────────────────
        group.MapGet("/ensure/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 Ensure validates the success value. If the predicate is false,
            // the chain short-circuits to the error without calling Bind/Map.
            var result = await svc.GetByIdAsync(id)
                .EnsureAsync(
                    p => p.Stock > 0,
                    p => new UnionError.Conflict($"Product '{p.Name}' is out of stock."));

            return result.ToHttpResult();
        })
        .WithSummary("Ensure — validate success value in the chain")
        .WithDescription(
            "EnsureAsync(predicate, errorFactory) checks the success value. " +
            "If predicate returns false, the chain becomes an error. " +
            "Try id=1 (in stock → 200), id=3 (out of stock → 409).");

        // ── Ensure + Bind chain — null guard ──────────────────────────────────
        group.MapGet("/ensure-chain/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 Ensure prevents null from reaching BindAsync.
            // Without Ensure, a null success would cause NullReferenceException in Bind.
            var result = await svc.FindByIdOrNullAsync(id)
                .EnsureAsync(
                    p => p is not null,
                    _ => new UnionError.NotFound("Product"))
                .BindAsync(p => ValueTask.FromResult(Union.Ok(new
                {
                    p!.Id,
                    p.Name,
                    Label = $"{p.Name} (${p.Price:F2})"
                })));

            return result.ToHttpResult();
        })
        .WithSummary("Ensure + Bind chain — null safety")
        .WithDescription(
            "Ensure prevents null values from reaching downstream Bind/Map. " +
            "Without Ensure, a null success crashes. Try id=1 (200), id=999 (404).");

        // ── Switch: void side effects on both branches ────────────────────────
        group.MapGet("/switch/{id:int}", async (int id, ProductService svc, [FromServices] ILogger<Program> logger) =>
        {
            // 💡 Switch is like Match but returns void — perfect for logging both paths.
            var result = await svc.GetByIdAsync(id);

            result.Switch(
                onOk: p => logger.LogInformation("✅ Product loaded: {Name} (${Price})", p.Name, p.Price),
                onError: err => logger.LogWarning("❌ Failed to load product {Id}: {Error}", id, err));

            return result.ToHttpResult();
        })
        .WithSummary("Switch — void Match for side effects")
        .WithDescription(
            "Switch(onOk, onError) executes one of two branches without returning a value. " +
            "Perfect for logging both success and failure. Check server logs after calling. " +
            "Try id=1 (logs success), id=999 (logs error).");

        // ── ToFail: shorthand error creation ──────────────────────────────────
        group.MapGet("/to-fail", () =>
        {
            // 💡 ToFail<T>() is a shorthand for Union.Fail<T>(error)
            UnionError error = new UnionError.Conflict("Item already exists");
            Rail<Product> rail = error.ToFail<Product>();

            return rail.ToHttpResult();
        })
        .WithSummary("ToFail — shorthand for creating failed Rail")
        .WithDescription(
            "Instead of Union.Fail<T>(new UnionError.Conflict(...)), " +
            "use error.ToFail<T>() for cleaner code. Always returns 409.");

        // ── SystemFailure(string): no Exception wrapping needed ───────────────
        group.MapGet("/system-failure-message", () =>
        {
            // 💡 Before: new UnionError.SystemFailure(new InvalidOperationException("msg"))
            // After:  new UnionError.SystemFailure("msg")
            Rail<Product> rail = Union.Fail<Product>(new UnionError.SystemFailure("Database connection lost"));

            return rail.ToHttpResult();
        })
        .WithSummary("SystemFailure(string) — message constructor")
        .WithDescription(
            "Create SystemFailure with just a message string. " +
            "Internally wraps in InvalidOperationException. Always returns 500.");

        return app;
    }

    /// <summary>
    /// A helper method that returns a nullable product — simulates a method
    /// that might return a successful Rail with a null value.
    /// </summary>
    private static ValueTask<Rail<Product?>> FindByIdOrNullAsync(this ProductService svc, int id)
    {
        return svc.GetByIdAsync(id)
            .RecoverAsync<Product?, UnionError.NotFound>(_ => (Product?)null);
    }
}
