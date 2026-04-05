# UnionRailway

[![Build](https://img.shields.io/github/actions/workflow/status/yourorg/UnionRailway/ci.yml?branch=main&label=build)](https://github.com/yourorg/UnionRailway)
[![NuGet](https://img.shields.io/nuget/v/UnionRailway.svg)](https://www.nuget.org/packages/UnionRailway)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-55%20passing-brightgreen)](tests/UnionRailway.Tests)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com)

> **Pragmatic, production-ready Railway-Oriented Programming for C# — without the functional jargon.**

UnionRailway gives you typed, composable error handling inspired by Rust's `Result<T, E>` — using clean C# patterns you already know: `out` parameters, `if`/`return`, and lambda callbacks. No `Bind`, no `FlatMap`, no monad tutorials required.

---

## Table of Contents

- [Why UnionRailway?](#why-unionrailway)
- [Installation](#installation)
- [Core Concepts](#core-concepts)
  - [UnionError — the universal error type](#unionerror--the-universal-error-type)
  - [Result\<T\> — your return type](#resultt--your-return-type)
- [Developer Experience (DX)](#developer-experience-dx)
  - [Early Return with `IsSuccess`](#early-return-with-issuccess)
  - [`Unwrap()` — Rust-style value access](#unwrap--rust-style-value-access)
  - [`UnwrapOrDefault()` — safe fallback](#unwrapordefault--safe-fallback)
  - [`Match()` — clean branching](#match--clean-branching)
  - [`Result.Combine(...)` — aggregate multiple results](#resultcombine--aggregate-multiple-results)
- [Adapters](#adapters)
  - [UnionRailway.AspNetCore — HTTP Results & ProblemDetails](#unionrailwayaspnetcore--http-results--problemdetails)
  - [UnionRailway.EntityFrameworkCore — Smart EF Core Queries](#unionrailwayentityframeworkcore--smart-ef-core-queries)
  - [UnionRailway.HttpClient — Automatic HTTP ProblemDetails Parsing](#unionrailwayhttpclient--automatic-http-problemdetails-parsing)
- [Real-World Example: Minimal API Endpoint](#real-world-example-minimal-api-endpoint)
- [Error Taxonomy Reference](#error-taxonomy-reference)
- [Testing Philosophy](#testing-philosophy)

---

## Why UnionRailway?

Most error-handling libraries for C# are ports of Haskell/F# concepts. They require developers to learn `Bind`, `Map`, `Tap`, and monad laws. **UnionRailway takes a different approach:**

| Concern                        | Traditional FP libs      | UnionRailway                     |
|-------------------------------|--------------------------|----------------------------------|
| Accessing a value             | `.Map(x => ...)` chain   | `if (!result.IsSuccess(out var x, out var err))` |
| Checking for failure           | `result.IsFailure`       | `!result.IsOk`                   |
| Safe access without throwing  | `.GetOrElse(default)`    | `.UnwrapOrDefault(default)`      |
| Explicit unsafe access        | `.Value`                 | `.Unwrap()` — throws `UnwrapException` |
| Branching                     | `.Match(ok, fail)`       | `.Match(onOk, onError)`          |
| HTTP translation              | Manual switch expression | `.ToHttpResult()` — RFC 9457     |
| EF Core null-safety           | Manual null checks       | `.FirstOrDefaultAsUnionAsync()`  |
| HTTP client error parsing     | Manual status code checks| `.GetFromJsonAsUnionAsync<T>()`  |

---

## Installation

```bash
# Core library
dotnet add package UnionRailway

# Adapters (install only what you need)
dotnet add package UnionRailway.AspNetCore
dotnet add package UnionRailway.EntityFrameworkCore
dotnet add package UnionRailway.HttpClient
```

---

## Core Concepts

### UnionError — the universal error type

Every failure in your application maps to one of six well-known cases:

```csharp
// Create errors using the static factory methods
var notFound     = UnionError.NotFound("User");
var conflict     = UnionError.Conflict("Email already registered");
var unauthorized = UnionError.Unauthorized();
var forbidden    = UnionError.Forbidden("Admin role required");
var validation   = UnionError.Validation([
    ("Email",    ["Must be a valid email address"]),
    ("Password", ["Minimum 8 characters", "Must contain a digit"])
]);
var systemError  = UnionError.SystemFailure(exception);
```

Inspect them with a `switch` expression when needed:

```csharp
string message = error.Kind switch
{
    UnionErrorKind.NotFound      => $"Could not find: {error.Resource}",
    UnionErrorKind.Conflict      => $"Conflict: {error.Reason}",
    UnionErrorKind.Unauthorized  => "Please log in.",
    UnionErrorKind.Forbidden     => $"Access denied: {error.Reason}",
    UnionErrorKind.Validation    => "Fix the highlighted fields.",
    UnionErrorKind.SystemFailure => "Something went wrong. Try again later.",
    _                            => "Unknown error"
};
```

### Result\<T\> — your return type

Use `Result<T>` as the return type for any operation that can fail:

```csharp
// Service method signatures
public Result<User>  GetUser(Guid id) { ... }
public Result<Order> PlaceOrder(PlaceOrderCommand cmd) { ... }

// Async variants work naturally
public Task<Result<User>>  GetUserAsync(Guid id, CancellationToken ct) { ... }
public Task<Result<Order>> PlaceOrderAsync(PlaceOrderCommand cmd, CancellationToken ct) { ... }
```

Construct results:

```csharp
// Success
return Result.Ok(user);

// Failure
return Result.Error<User>(UnionError.NotFound("User"));

// Implicit conversion also works
Result<User> result = user;                        // Ok
Result<User> result = UnionError.Unauthorized();   // Error
```

---

## Developer Experience (DX)

### Early Return with `IsSuccess`

The **primary pattern** for consuming results. Mirrors the Rust `?` operator feel in idiomatic C#:

```csharp
public async Task<Result<OrderDto>> GetOrderDetailsAsync(Guid orderId, Guid userId)
{
    // Step 1 — find the user
    if (!await userService.GetUserAsync(userId).ConfigureAwait(false) is var userResult
        || !userResult.IsSuccess(out var user, out var userError))
        return userError;

    // Step 2 — find the order (only reached if user was found)
    if (!await orderRepo.GetOrderAsync(orderId).ConfigureAwait(false) is var orderResult
        || !orderResult.IsSuccess(out var order, out var orderError))
        return orderError;

    // Step 3 — authorisation check
    if (order.OwnerId != user.Id)
        return UnionError.Forbidden("You do not own this order.");

    return Result.Ok(OrderDto.From(order));
}
```

The cleaner, more common form:

```csharp
public async Task<Result<OrderDto>> GetOrderDetailsAsync(Guid orderId, Guid userId)
{
    var userResult = await userService.GetUserAsync(userId);
    if (!userResult.IsSuccess(out var user, out var userError))
        return userError;   // propagate — no verbose wrapping needed

    var orderResult = await orderRepo.GetOrderAsync(orderId);
    if (!orderResult.IsSuccess(out var order, out var orderError))
        return orderError;

    if (order.OwnerId != user.Id)
        return UnionError.Forbidden("You do not own this order.");

    return Result.Ok(OrderDto.From(order));
}
```

### `Unwrap()` — Rust-style value access

Useful in tests, quick scripts, or code paths where you **know** a result must be Ok:

```csharp
// Throws UnwrapException if the result is an Error
var user = Result.Ok(new User("Alice")).Unwrap();
Console.WriteLine(user.Name); // Alice

// Catching the failure explicitly
try
{
    var value = failingResult.Unwrap();
}
catch (UnwrapException ex)
{
    logger.LogError("Unexpected failure: {Error}", ex.Error);
    throw;
}
```

### `UnwrapOrDefault()` — safe fallback

Never throws. Returns the value on success, or the provided default on failure:

```csharp
// Returns -1 if the price could not be fetched
var price = await FetchPriceAsync(productId)
    .ContinueWith(t => t.Result.UnwrapOrDefault(-1m));

// Useful for read-optional patterns
var cachedUser = cacheResult.UnwrapOrDefault(null);
```

### `Match()` — clean branching

A lambda-based alternative to `switch` expressions; ideal for transforming results inline:

```csharp
// Transform a result into an HTTP-agnostic DTO
var response = orderResult.Match(
    onOk:    order => new { Success = true,  Data  = OrderDto.From(order) },
    onError: err   => new { Success = false, Error = err.ToString() });

// Inside a Minimal API endpoint (concise form)
app.MapGet("/users/{id}", async (Guid id, UserService svc) =>
{
    var result = await svc.GetUserAsync(id);
    return result.Match(
        onOk:    user => Results.Ok(user),
        onError: err  => err.ToHttpResult());
});
```

### `Result.Combine(...)` — aggregate multiple results

Run several independent validations or queries and get them all back at once. When all succeed, you get a tuple. When any fail, all field-level errors are merged into a single `Validation` error:

```csharp
var nameResult  = ValidateName(command.Name);
var emailResult = ValidateEmail(command.Email);
var ageResult   = ValidateAge(command.Age);

// All succeed → tuple
// Any fail → merged Validation error with all field messages
var combined = Result.Combine(nameResult, emailResult, ageResult);

if (!combined.IsSuccess(out var (name, email, age), out var validationError))
    return validationError.ToHttpResult(); // returns 400 with all field errors

var user = new User(name, email, age);
```

---

## Adapters

### UnionRailway.AspNetCore — HTTP Results & ProblemDetails

Translates any `Result<T>` or `UnionError` directly into an `IResult` for Minimal APIs or Controller helpers. All error responses follow **RFC 9457 Problem Details**:

| `UnionError` kind  | HTTP status              |
|--------------------|--------------------------|
| `NotFound`         | `404 Not Found`          |
| `Conflict`         | `409 Conflict`           |
| `Unauthorized`     | `401 Unauthorized`       |
| `Forbidden`        | `403 Forbidden`          |
| `Validation`       | `400 Bad Request` (with `errors` map) |
| `SystemFailure`    | `500 Internal Server Error` |

```csharp
// Simple 200 / error response
app.MapGet("/users/{id:guid}", async (Guid id, UserService svc) =>
{
    var result = await svc.GetAsync(id);
    return result.ToHttpResult();
    // Ok(user) → 200 { ... }
    // Error(NotFound) → 404 { "title": "Not Found", "detail": "..." }
});

// Return 201 Created with a Location header on success
app.MapPost("/users", async (CreateUserCommand cmd, UserService svc) =>
{
    var result = await svc.CreateAsync(cmd);
    return result.ToHttpResult(createdUri: $"/users/{result.IsOk ? result.Unwrap().Id : Guid.Empty}");
    // Ok(user) → 201 Created  Location: /users/abc-123
    // Error(Validation) → 400 { "errors": { "Email": ["Invalid"] } }
});

// Translate a bare error
app.MapDelete("/users/{id:guid}", async (Guid id, UserService svc) =>
{
    var error = await svc.DeleteAsync(id);
    return error?.ToHttpResult() ?? Results.NoContent();
});
```

**Validation problem details response shape (RFC 9457):**

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["Must be a valid email address"],
    "Password": ["Minimum 8 characters"]
  }
}
```

---

### UnionRailway.EntityFrameworkCore — Smart EF Core Queries

Eliminates `if (entity == null)` checks and wraps database exceptions automatically:

```csharp
// Before — manual null check + exception handling
public async Task<Result<Product>> GetProductAsync(Guid id)
{
    try
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return UnionError.NotFound("Product");
        return Result.Ok(product);
    }
    catch (Exception ex)
    {
        return UnionError.SystemFailure(ex);
    }
}

// After — one line
public Task<Result<Product>> GetProductAsync(Guid id) =>
    db.Products.FirstOrDefaultAsUnionAsync("Product", p => p.Id == id);
```

**`SaveChangesAsUnionAsync`** — detects concurrency and unique-constraint violations:

```csharp
db.Products.Add(newProduct);
var saveResult = await db.SaveChangesAsUnionAsync();

if (!saveResult.IsSuccess(out var rowsAffected, out var saveError))
{
    // Conflict → 409 (e.g. duplicate SKU)
    // SystemFailure → 500
    return saveError.ToHttpResult();
}

logger.LogInformation("Saved {Rows} rows", rowsAffected);
```

| Exception                      | Mapped to                       |
|--------------------------------|---------------------------------|
| `DbUpdateConcurrencyException` | `Conflict` — concurrency message |
| `DbUpdateException` (unique)   | `Conflict` — constraint message |
| `DbUpdateException` (other)    | `SystemFailure`                 |
| Any other `Exception`          | `SystemFailure`                 |

---

### UnionRailway.HttpClient — Automatic HTTP ProblemDetails Parsing

Wraps `System.Net.Http.HttpClient` calls and maps status codes to typed `Result<T>` values. **400 responses are automatically parsed as RFC 9457/7807 problem details** and surfaced as `UnionError.Validation` with the full field-error map:

```csharp
// GET
var result = await httpClient.GetFromJsonAsUnionAsync<UserDto>("/api/users/1");

if (!result.IsSuccess(out var user, out var error))
{
    // 401 → UnionError.Unauthorized()
    // 403 → UnionError.Forbidden("...")
    // 404 → UnionError.NotFound("Not Found")
    // 400 → UnionError.Validation({ "Email": ["Invalid"] })  ← parsed automatically
    // 500 → UnionError.SystemFailure(HttpRequestException)
    return error.ToHttpResult();
}

// POST
var createResult = await httpClient.PostAsJsonAsUnionAsync<CreatedUserDto>(
    "/api/users",
    new { Name = "Alice", Email = "alice@example.com" });

// PUT
var updateResult = await httpClient.PutAsJsonAsUnionAsync<UserDto>(
    $"/api/users/{id}",
    new { Name = "Alice Updated" });

// DELETE
var deleteResult = await httpClient.DeleteAsUnionAsync($"/api/users/{id}");
```

**Status code mapping:**

| HTTP Status | `UnionError` produced                    |
|-------------|------------------------------------------|
| `200 / 201` | `Ok<T>` (deserialized JSON body)         |
| `400`       | `Validation` (RFC 7807 errors parsed)    |
| `401`       | `Unauthorized`                           |
| `403`       | `Forbidden` (detail from Problem body)   |
| `404`       | `NotFound` (detail from Problem body)    |
| `409`       | `Conflict` (detail from Problem body)    |
| other 4xx/5xx | `SystemFailure` (with status message)  |
| Network/timeout | `SystemFailure` (wrapped exception)  |

---

## Real-World Example: Minimal API Endpoint

A complete Minimal API endpoint demonstrating the full pipeline from HTTP client → service → EF Core → ASP.NET Core response:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

// GET /orders/{id}
app.MapGet("/orders/{id:guid}", async (
    Guid id,
    ClaimsPrincipal user,
    OrderService svc) =>
{
    var result = await svc.GetOrderAsync(id, user.GetUserId());

    // Ok(order) → 200 { ... }
    // Error(NotFound) → 404 problem details
    // Error(Forbidden) → 403 problem details
    return result.ToHttpResult();
});

// POST /orders
app.MapPost("/orders", async (
    PlaceOrderCommand cmd,
    ClaimsPrincipal user,
    OrderService svc) =>
{
    var result = await svc.PlaceOrderAsync(cmd, user.GetUserId());

    return result.ToHttpResult(
        createdUri: result.IsOk ? $"/orders/{result.Unwrap().Id}" : null);
    // Ok(order) → 201 Created  Location: /orders/abc-123
    // Error(Validation) → 400 { "errors": { "Quantity": ["Must be > 0"] } }
    // Error(Conflict) → 409 (e.g. item out of stock)
});
```

```csharp
// OrderService.cs
public sealed class OrderService(AppDbContext db, IHttpClientFactory httpClientFactory)
{
    public async Task<Result<Order>> GetOrderAsync(Guid orderId, Guid userId)
    {
        // Smart EF Core query — null → NotFound, DbException → SystemFailure
        var orderResult = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsUnionAsync("Order", o => o.Id == orderId);

        if (!orderResult.IsSuccess(out var order, out var error))
            return error;

        if (order.UserId != userId)
            return UnionError.Forbidden("You do not own this order.");

        return Result.Ok(order);
    }

    public async Task<Result<Order>> PlaceOrderAsync(PlaceOrderCommand cmd, Guid userId)
    {
        // Parallel validation using Combine
        var qtyResult  = ValidateQuantity(cmd.Quantity);
        var skuResult  = ValidateSku(cmd.Sku);

        var validationResult = Result.Combine(qtyResult, skuResult);
        if (!validationResult.IsSuccess(out var (quantity, sku), out var validationError))
            return validationError;

        // Fetch pricing from external service — automatic HTTP error mapping
        var httpClient = httpClientFactory.CreateClient("CatalogService");
        var priceResult = await httpClient.GetFromJsonAsUnionAsync<PriceDto>(
            $"/catalog/{sku}/price");

        if (!priceResult.IsSuccess(out var priceDto, out var priceError))
            return priceError; // propagates NotFound / SystemFailure transparently

        var order = new Order(userId, sku, quantity, priceDto.UnitPrice);
        db.Orders.Add(order);

        // Save — unique constraint → Conflict, concurrency → Conflict
        var saveResult = await db.SaveChangesAsUnionAsync();
        if (!saveResult.IsSuccess(out _, out var saveError))
            return saveError;

        return Result.Ok(order);
    }

    private static Result<int> ValidateQuantity(int qty) =>
        qty > 0
            ? Result.Ok(qty)
            : Result.Error<int>(
                UnionError.Validation([("Quantity", ["Must be greater than 0"])]));

    private static Result<string> ValidateSku(string? sku) =>
        !string.IsNullOrWhiteSpace(sku)
            ? Result.Ok(sku)
            : Result.Error<string>(
                UnionError.Validation([("Sku", ["SKU is required"])]));
}
```

---

## Error Taxonomy Reference

| Kind            | Factory                                          | Typical cause                              |
|-----------------|--------------------------------------------------|--------------------------------------------|
| `NotFound`      | `UnionError.NotFound(resource)`                  | DB query returned null; 404 from API       |
| `Conflict`      | `UnionError.Conflict(reason)`                    | Duplicate key; optimistic concurrency      |
| `Unauthorized`  | `UnionError.Unauthorized()`                      | Missing / expired token                   |
| `Forbidden`     | `UnionError.Forbidden(reason)`                   | Insufficient permissions                  |
| `Validation`    | `UnionError.Validation(fields)`                  | Input validation failures                  |
| `SystemFailure` | `UnionError.SystemFailure(exception)`            | Unhandled exception; network failure       |

---

## Testing Philosophy

UnionRailway is designed to be trivially testable. Results are simple sealed classes — no mocking required:

```csharp
// Assert success
var result = await svc.GetUserAsync(knownId);
Assert.True(result.IsOk);
Assert.Equal("Alice", result.Unwrap().Name);

// Assert specific error kind
var result = await svc.GetUserAsync(unknownId);
Assert.False(result.IsOk);
result.IsSuccess(out _, out var error);
Assert.Equal(UnionErrorKind.NotFound, error.Kind);
Assert.Equal("User", error.Resource);

// Assert validation fields
var result = svc.Validate(invalidCommand);
result.IsSuccess(out _, out var err);
Assert.Equal(UnionErrorKind.Validation, err.Kind);
Assert.Contains("Email", err.Fields!.Keys);
```

---

## License

MIT © 2025 — see [LICENSE](LICENSE) for details.
