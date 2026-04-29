namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 03 — Error Types: "Something went wrong" is not enough!
///
/// In the previous step we saw that Rail<T> carries an error. But what kind of error?
/// Is it enough to say "return 404"? Is it a NotFound, Unauthorized, or Validation error?
/// Carrying all this merely as a string ruins type-safety.
///
/// ❓ Question: What if the caller wants to behave differently based on the error type?
///          String comparisons? Magic numbers?
///
/// UnionError has 7 semantic error types: NotFound, Conflict, Unauthorized,
/// Forbidden, Validation, SystemFailure, and Custom. Each maps automatically
/// to its own HTTP status code. In the next step, we will see how to handle these errors.
/// </summary>
public static class Step03_ErrorTypes
{
    private static readonly string[] pairs = new[] { "Must be greater than 0", "Must be a valid decimal" };

    public static RouteGroupBuilder MapStep03(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step03").WithTags("03 - Error Types");

        group.MapGet("/not-found", () =>
        {
            Rail<string> result = Union.Fail<string>(new UnionError.NotFound("Product"));

            return result.ToHttpResult();
        })
        .WithSummary("NotFound → 404 Problem Details")
        .WithDescription("UnionError.NotFound maps to HTTP 404 with RFC 7807 body.");

        group.MapGet("/conflict", () =>
        {
            Rail<string> result = Union.Fail<string>(new UnionError.Conflict("SKU 'LAP-001' already exists"));

            return result.ToHttpResult();
        })
        .WithSummary("Conflict → 409 Problem Details")
        .WithDescription("UnionError.Conflict maps to HTTP 409.");

        group.MapGet("/unauthorized", () =>
        {
            Rail<string> result = Union.Fail<string>(new UnionError.Unauthorized());

            return result.ToHttpResult();
        })
        .WithSummary("Unauthorized → 401 Problem Details")
        .WithDescription("UnionError.Unauthorized maps to HTTP 401.");

        group.MapGet("/forbidden", () =>
        {
            Rail<string> result = Union.Fail<string>(new UnionError.Forbidden("You don't own this resource"));

            return result.ToHttpResult();
        })
        .WithSummary("Forbidden → 403 Problem Details")
        .WithDescription("UnionError.Forbidden maps to HTTP 403.");

        group.MapGet("/validation", () =>
        {
            Rail<string> result = Union.Fail<string>(UnionError.CreateValidation(
            [
                ("Name",     ["Name is required"]),
                ("Price",    pairs),
                ("Sku",      ["SKU is required"])
            ]));

            return result.ToHttpResult();
        })
        .WithSummary("Validation → 422 Problem Details with field errors")
        .WithDescription("UnionError.Validation maps to HTTP 422 with per-field error arrays.");

        group.MapGet("/system-failure", () =>
        {
            Rail<string> result = Union.Fail<string>(
                new UnionError.SystemFailure(new InvalidOperationException("Database connection lost")));

            return result.ToHttpResult();
        })
        .WithSummary("SystemFailure → 500 Problem Details")
        .WithDescription("UnionError.SystemFailure maps to HTTP 500. Wraps an actual Exception.");

        group.MapGet("/custom/{statusCode:int}", (int statusCode) =>
        {
            // 💡 Custom lets you define domain-specific errors with any HTTP status code
            Rail<string> result = Union.Fail<string>(new UnionError.Custom(
                Code: "RATE_LIMIT_EXCEEDED",
                Message: "Too many requests. Please slow down.",
                StatusCode: statusCode,
                Extensions: new Dictionary<string, object>
                {
                    ["retryAfter"] = 30,
                    ["limit"]      = 100
                }));

            return result.ToHttpResult();
        })
        .WithSummary("Custom → any status code + metadata")
        .WithDescription(
            "UnionError.Custom accepts any HTTP status code and optional key/value Extensions. " +
            "Try statusCode=429 (rate limit), 402 (payment required), etc.");

        return app;
    }
}
