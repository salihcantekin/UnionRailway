using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 04 — How do we consume Rail<T>?
///
/// We have a typed Rail<T>. How do we process this result in the endpoint?
/// if/else? switch? Every team has their own style, and should be able to choose.
///
/// ❓ Question: Which approach is the most readable? Which suits your team best?
///
/// We offer three different styles: IsSuccess (early return), Match (functional style,
/// enforces handling both branches), and Error property (familiar null-check habit).
/// They all achieve the same thing, pick according to your style preference. In the
/// next step, we'll look at chaining multiple operations.
/// </summary>
public static class Step04_PatternMatching
{
    public static RouteGroupBuilder MapStep04(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step04").WithTags("04 - Pattern Matching");

        // ── Style 1: IsSuccess (most common, early-return friendly) ───────────
        group.MapGet("/style1-issuccess/{id:int}", async Task<IResult> (int id, ProductService svc) =>
        {
            // 💡 IsSuccess = early return pattern. Clean and explicit.

            var result = await svc.GetByIdAsync(id);

            if (!result.IsSuccess(out var product, out var error))
            {
                return error.GetValueOrDefault().ToHttpResult();
            }

            return Results.Ok(new { Style = "IsSuccess", Product = product });
        })
        .WithSummary("Style 1: IsSuccess early-return pattern")
        .WithDescription("Most idiomatic: deconstructs value + error, early-returns on failure. Try id=1 or id=999.");




        // ── Style 2: Match (functional / expression-body friendly) ────────────
        group.MapGet("/style2-match/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 Match forces you to handle BOTH branches — nothing slips through.
            var result = await svc.GetByIdAsync(id);

            return result.Match(
                onOk: product => Results.Ok(new { Style = "Match", Product = product }),
                onError: error => error.ToHttpResult());
        })
        .WithSummary("Style 2: Match — handles both branches explicitly")
        .WithDescription("Functional style. Compiler forces you to handle success AND error. Try id=1 or id=999.");






        // ── Style 3: Error property check ─────────────────────────────────────
        group.MapGet("/style3-error-prop/{id:int}", async Task<IResult> (int id, ProductService svc) =>
        {
            // 💡 Error property is null when success — familiar null-check idiom
            var result = await svc.GetByIdAsync(id);
            if (result.Error is not null)
            {
                return result.Error.GetValueOrDefault().ToHttpResult();
            }

            // Safe to access after the check — you know it's success here
            return Results.Ok(new { Style = "ErrorProperty", Product = result.Unwrap() });
        })
        .WithSummary("Style 3: Error property — familiar null-check idiom")
        .WithDescription("Check result.Error is not null, then Unwrap() the success value. Try id=1 or id=999.");





        // ── Switch expression on error type ───────────────────────────────────
        group.MapGet("/switch-on-error/{id:int}", async Task<IResult> (int id, ProductService svc) =>
        {
            // 💡 Pattern match on the SPECIFIC error type for fine-grained handling
            var result = await svc.GetByIdAsync(id);
            if (result.IsSuccess(out var product, out var error))
            {
                return Results.Ok(product);
            }

            var message = error.GetValueOrDefault().Value switch
            {
                UnionError.NotFound nf => $"Resource missing: {nf.Resource}",
                UnionError.Conflict c => $"Conflict: {c.Reason}",
                UnionError.Unauthorized => "Please log in",
                UnionError.Forbidden f => $"Access denied: {f.Reason}",
                UnionError.Validation v => $"{v.Fields.Count} field(s) invalid",
                UnionError.SystemFailure sf => $"System error: {sf.Ex.Message}",
                UnionError.Custom c => $"[{c.Code}] {c.Message}",
                _ => "Unknown error"
            };

            return Results.Problem(message, statusCode: 400);
        })
        .WithSummary("Switch on specific error type")
        .WithDescription(
            "Switch expression on error.Value to get TYPED access to each error variant's properties. " +
            "Try id=1 (success), id=999 (NotFound with typed resource name).");

        return app;
    }
}
