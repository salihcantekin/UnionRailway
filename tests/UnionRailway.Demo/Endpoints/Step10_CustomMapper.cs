using UnionRailway.AspNetCore;
using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 10 — Default Error Messages are not enough, I want complete customization!
///
/// In the previous step, we enriched ProblemDetails with configureProblem.
/// But what if we want an entirely different response shape for a NotFound error?
/// What if we need to add a specialized extension field for a 429 Rate Limit error?
///
/// ❓ Question: Is it possible to completely override the default mapping for specific
///          error types, while falling back to the default for everything else?
///
/// If IUnionErrorMapper.TryMap() returns null, the default RFC 7807 behavior kicks in.
/// If it yields a non-null IResult, that result is used instead. It registers directly with
/// DI and can inject anywhere. Next step: EF Core integration.
/// </summary>
public static class Step10_CustomMapper
{
    // 💡 Implement IUnionErrorMapper to take over specific error types.
    //    Return null → fall through to default mapping.
    public sealed class DemoErrorMapper : IUnionErrorMapper
    {
        public IResult? TryMap(UnionError error) => error.Value switch
        {
            UnionError.NotFound nf => Results.Problem(
                detail:     $"We searched everywhere but couldn't find '{nf.Resource}'.",
                statusCode: 404,
                title:      "Resource Not Found",
                extensions: new Dictionary<string, object?>
                {
                    ["resource"]   = nf.Resource,
                    ["suggestion"] = "Check the identifier and try again"
                }),

            UnionError.Custom { Code: "RATE_LIMIT_EXCEEDED" } c => Results.Problem(
                detail:     c.Message,
                statusCode: 429,
                title:      "Rate Limited",
                extensions: new Dictionary<string, object?> { ["retryAfter"] = 30 }),

            _ => null  // fall back to default RFC 7807 mapping for all other types
        };
    }

    public static RouteGroupBuilder MapStep10(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step10").WithTags("10 - Custom Error Mapper");

        var mapper = new DemoErrorMapper();

        // ── NotFound with custom mapper ────────────────────────────────────────
        group.MapGet("/products/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 Pass the mapper directly per-endpoint
            return (await svc.GetByIdAsync(id)).ToHttpResult(errorMapper: mapper);
        })
        .WithSummary("Custom NotFound message via IUnionErrorMapper")
        .WithDescription(
            "DemoErrorMapper produces a richer 404 with suggestion field. " +
            "Try id=999 to see custom error shape vs Step07 (default mapping).");





        // ── Custom error type intercepted ─────────────────────────────────────
        group.MapGet("/rate-limited", () =>
        {
            Rail<string> result = Union.Fail<string>(new UnionError.Custom(
                Code:       "RATE_LIMIT_EXCEEDED",
                Message:    "You've exceeded 100 requests/minute.",
                StatusCode: 429));

            return result.ToHttpResult(errorMapper: mapper);
        })
        .WithSummary("Custom.Code 'RATE_LIMIT_EXCEEDED' intercepted by mapper")
        .WithDescription("Mapper intercepts specific Custom.Code and returns 429 with retryAfter.");





        // ── Conflict falls through to default (mapper returns null) ───────────
        group.MapGet("/conflict-fallthrough", () =>
        {
            Rail<string> result = Union.Fail<string>(new UnionError.Conflict("Duplicate SKU"));

            // 💡 Mapper returns null for Conflict → default RFC 7807 409 kicks in
            return result.ToHttpResult(errorMapper: mapper);
        })
        .WithSummary("Conflict falls through — mapper returns null")
        .WithDescription(
            "DemoErrorMapper doesn't handle Conflict → returns null → " +
            "default RFC 7807 mapping produces 409. Mix custom + default effortlessly.");





        // ── Global DI mapper (registered in Program.cs) ───────────────────────
        group.MapGet("/products/{id:int}/di-mapper", async (int id, ProductService svc, [FromServices] IUnionErrorMapper? diMapper) =>
        {
            // 💡 Inject IUnionErrorMapper from DI — registered globally in Program.cs
            return (await svc.GetByIdAsync(id)).ToHttpResult(errorMapper: diMapper);
        })
        .WithSummary("DI-registered mapper — inject IUnionErrorMapper from container")
        .WithDescription(
            "IUnionErrorMapper registered via AddRailway<DemoErrorMapper>() in Program.cs — " +
            "inject it anywhere and pass to ToHttpResult(). Try id=999.");

        return app;
    }
}
