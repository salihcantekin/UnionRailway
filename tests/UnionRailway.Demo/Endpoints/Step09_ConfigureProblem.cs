using System.Diagnostics;
using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 09 — I want to add traceId to Error Responses!
///
/// Our errors now automatically conform to the RFC 7807 format. But in production,
/// every error response usually needs a traceId or requestId so we can match them
/// in our logging system. Do we inject this manually in every endpoint?
///
/// ❓ Question: Can we apply this enrichment in one central place for all errors at once?
///          There should be zero overhead on successful responses.
///
/// The configureProblem callback executes purely on error paths — zero overhead on success.
/// It can be centrally managed globally via AddRailway(options => options.ConfigureProblem = ...).
/// In the next step, we will fully hijack the error → HTTP mapping behavior.
/// </summary>
public static class Step09_ConfigureProblem
{
    public static RouteGroupBuilder MapStep09(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step09").WithTags("09 - Configure ProblemDetails");

        // ── Add traceId to every error response ───────────────────────────────
        group.MapGet("/with-traceid/{id:int}", async (int id, ProductService svc) =>
        {
            // 💡 configureProblem is called ONLY on error paths — zero overhead on success
            return (await svc.GetByIdAsync(id)).ToHttpResult(
                configureProblem: pd =>
                {
                    pd.Extensions["traceId"]   = Activity.Current?.Id ?? "no-trace";
                    pd.Extensions["requestId"] = Guid.NewGuid().ToString("N")[..8];
                });
        })
        .WithSummary("configureProblem — add traceId to error responses")
        .WithDescription(
            "configureProblem callback enriches ProblemDetails with traceId and requestId. " +
            "Only called on error paths — no overhead on success. Try id=999 to see the extensions.");






        // ── Override title and detail ──────────────────────────────────────────
        group.MapGet("/custom-message/{id:int}", async (int id, ProductService svc) =>
        {
            return (await svc.GetByIdAsync(id)).ToHttpResult(
                configureProblem: pd =>
                {
                    pd.Title  = "Oops! We couldn't find that.";
                    pd.Detail = "The product you're looking for has been moved or deleted.";
                    pd.Extensions["supportUrl"] = "https://support.example.com";
                });
        })
        .WithSummary("configureProblem — override title, detail, add support URL")
        .WithDescription("Fully customize the ProblemDetails shape per-endpoint. Try id=999.");








        // ── Global configureProblem via AddRailway() options ──────────────────
        // 💡 No per-endpoint configureProblem needed — it's registered globally in Program.cs
        //    via builder.Services.AddRailway(options => options.ConfigureProblem = ...)
        //    Every endpoint automatically gets traceId injected by the global config!
        group.MapGet("/global-config/{id:int}", async (int id, ProductService svc) => (await svc.GetByIdAsync(id)).ToHttpResult())
            .WithSummary("Global configureProblem — registered in AddRailway() options")
            .WithDescription(
                "This endpoint has NO configureProblem parameter, yet the global traceId is still injected. " +
                "Registered once in AddRailway(options => options.ConfigureProblem = ...) in Program.cs. " +
                "Try id=999 to see the global traceId extension.");

        return app;
    }
}
