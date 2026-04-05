# UnionRailway

[![Build](https://img.shields.io/github/actions/workflow/status/salihcantekin/UnionRailway/ci.yml?branch=main&label=build)](https://github.com/salihcantekin/UnionRailway)
[![NuGet](https://img.shields.io/nuget/v/UnionRailway.svg)](https://www.nuget.org/packages/UnionRailway)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-54%20passing-brightgreen)](tests/UnionRailway.Tests)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com)

> **Pragmatic, production-ready Railway-Oriented Programming for C# — without the functional jargon.**

UnionRailway gives you typed, composable error handling inspired by Rust's `Result<T, E>` — using clean C# patterns you already know: `out` parameters, `if`/`return`, and lambda callbacks. No `Bind`, no `FlatMap`, no monad tutorials required.

The core return type is a plain **`(T Value, UnionError? Error)`** `ValueTuple` — a struct. There are zero extra heap allocations on the success path.

---

## Table of Contents

- [Why UnionRailway?](#why-unionrailway)
- [Installation](#installation)
- [Core Concepts](#core-concepts)
  - [UnionError — the universal error type](#unionerror--the-universal-error-type)
  - [Union — constructing results](#union--constructing-results)
- [Developer Experience (DX)](#developer-experience-dx)
  - [Early Return with `IsSuccess`](#early-return-with-issuccess)
  - [`Unwrap()` — throw on error](#unwrap--throw-on-error)
  - [`UnwrapOrDefault()` — safe fallback](#unwrapordefault--safe-fallback)
  - [`Match()` — clean branching](#match--clean-branching)
  - [`Union.Combine(...)` — aggregate two results](#unioncombine--aggregate-two-results)
  - [`UnionWrapper.RunAsync` — legacy bridge](#unionwrapperrunasync--legacy-bridge)
- [Adapters](#adapters)
  - [UnionRailway.AspNetCore — HTTP Results & ProblemDetails](#unionrailwayaspnetcore--http-results--problemdetails)
  - [UnionRailway.EntityFrameworkCore — Smart EF Core Queries](#unionrailwayentityframeworkcore--smart-ef-core-queries)
  - [UnionRailway.HttpClient — Automatic HTTP Error Mapping](#unionrailwayhttpclient--automatic-http-error-mapping)
- [Real-World Example: Order Service](#real-world-example-order-service)
- [Error Taxonomy Reference](#error-taxonomy-reference)
- [Playground](#playground)
- [Testing](#testing)

---

## Why UnionRailway?

Most error-handling libraries for C# are ports of Haskell/F# concepts. They require developers to learn `Bind`, `Map`, `Tap`, and monad laws. **UnionRailway takes a different approach:**

| Concern | Traditional approach | UnionRailway |
|---|---|---|
| Return type | `Task<T>` (throws) or `T?` (null) | `(T Value, UnionError? Error)` ValueTuple |
| Constructing success | `return value;` | `return Union.Ok(value);` |
| Constructing failure | `throw new NotFoundException(...)` | `return Union.Fail<T>(new UnionError.NotFound("x"));` |
| Check and access | `try/catch` or null guard | `if (!result.IsSuccess(out var v, out var err))` |
| Branching on result | Manual `if`/`switch` | `.Match(onOk: ..., onError: ...)` |
| HTTP translation | Manual status-code switch | `.ToHttpResult()` — RFC 7807 ProblemDetails |
| EF Core null-safety | `if (entity == null) throw ...` | `.FirstOrDefaultAsUnionAsync("Resource")` |
| HTTP client error parsing | Manual status-code inspection | `.GetFromJsonAsUnionAsync<T>()` |

---

## Installation

```bash
# Core library — required
dotnet add package UnionRailway

# Adapters — install only what you use
dotnet add package UnionRailway.AspNetCore
dotnet add package UnionRailway.EntityFrameworkCore
dotnet add package UnionRailway.HttpClient
```

---

## Core Concepts

### UnionError — the universal error type

`UnionError` is a **closed `abstract record`** with a `private protected` constructor, so the hierarchy is sealed to these six nested record cases:

```csharp
new UnionError.NotFound("User")             // resource not found
new UnionError.Conflict("Email taken")      // duplicate / concurrency
new UnionError.Unauthorized()               // not authenticated
new UnionError.Forbidden("Admin required")  // not authorised
new UnionError.SystemFailure(exception)     // unexpected exception

// Validation — use the static factory for convenient tuple-pair syntax:
UnionError.CreateValidation([
    ("Email",    ["Must be a valid email address"]),
    ("Password", ["Minimum 8 characters", "Must contain a digit"])
])
```

Inspect an error with an exhaustive `switch` expression — the compiler enforces completeness because the hierarchy is sealed:

```csharp
string message = error switch
{
    UnionError.NotFound nf      => $"'{nf.Resource}' could not be found",
    UnionError.Conflict c       => $"Conflict: {c.Reason}",
    UnionError.Unauthorized     => "Authentication required",
    UnionError.Forbidden f      => $"Access denied: {f.Reason}",
    UnionError.Validation v     => $"{v.Fields.Count} field(s) failed validation",
    UnionError.SystemFailure sf => $"System error: {sf.Ex.Message}",
    _                           => "Unknown error"
};
```

> **Note:** The `UnionErrorKind` enum is kept in the package for source compatibility but is marked `[Obsolete]`. Pattern-match on the record subtypes instead.

---

### Union — constructing results

Use the `Union` static class to construct the `(T Value, UnionError? Error)` tuple:

```csharp
// Success — value is set, Error is null
(int Value, UnionError? Error) result = Union.Ok(42);

// Failure — Value is default, Error is set
(int Value, UnionError? Error) result = Union.Fail<int>(new UnionError.NotFound("Order-99"));

// You can also use tuple literals directly — both are equivalent:
return (user, null);                              // success
return (default!, new UnionError.NotFound("x"));  // failure
```

Declare your service method signatures using the tuple return type:

```csharp
// Synchronous
public (User Value, UnionError? Error) GetUser(Guid id) { ... }

// Async
public async Task<(User Value, UnionError? Error)> GetUserAsync(Guid id, CancellationToken ct) { ... }

// Async, zero-allocation
public async ValueTask<(User Value, UnionError? Error)> GetUserAsync(Guid id, CancellationToken ct) { ... }
```

---

## Developer Experience (DX)

### Early Return with `IsSuccess`

The **primary pattern** — deconstructs the tuple and enables an early-return on failure:

```csharp
public async Task<(Order Value, UnionError? Error)> GetOrderDetailsAsync(Guid orderId, Guid userId)
{
    // Step 1 — look up the order; short-circuits on NotFound / SystemFailure
    var (order, orderErr) = await db.Orders
        .FirstOrDefaultAsUnionAsync("Order", o => o.Id == orderId);

    if (orderErr is not null) return Union.Fail<Order>(orderErr);

    // Step 2 — authorisation (only reached when order was found)
    if (order!.UserId != userId)
        return Union.Fail<Order>(new UnionError.Forbidden("You do not own this order."));

    return Union.Ok(order);
}
```

The `IsSuccess` overload gives you named variables for both outcomes:

```csharp
var result = await productSvc.GetByIdAsync(id);

if (!result.IsSuccess(out var product, out var err))
{
    // err is UnionError (NotFound, Conflict, …)
    return err!.ToHttpResult();   // AspNetCore adapter
}

// product is safely accessible here
Console.WriteLine(product!.Name);
```

### `Unwrap()` — throw on error

Useful in tests, seed scripts, or code paths that **guarantee** a success:

```csharp
// Throws UnwrapException if the result is a failure
var user = Union.Ok(new User("Alice")).Unwrap();

// Catching the failure explicitly
try
{
    var order = failingResult.Unwrap();
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
// Returns 0 if the result is an error
var price = priceResult.UnwrapOrDefault(0m);

// Useful for optional reads
var cached = cacheResult.UnwrapOrDefault(null);
```

### `Match()` — clean branching

Transforms a result into something else using two lambdas. Both branches must produce the same type:

```csharp
var message = orderResult.Match(
    onOk:    order => $"Order #{order.Id} confirmed — ${order.TotalPrice:F2}",
    onError: err   => err switch
    {
        UnionError.NotFound nf  => $"Order '{nf.Resource}' not found",
        UnionError.Forbidden f  => $"Access denied: {f.Reason}",
        _                       => $"Error: {err.GetType().Name}"
    });

Console.WriteLine(message);
```

Inside a Minimal API endpoint:

```csharp
app.MapGet("/orders/{id:guid}", async (Guid id, OrderService svc) =>
{
    var result = await svc.GetOrderAsync(id);
    return result.Match(
        onOk:    order => Results.Ok(order),
        onError: err   => err.ToHttpResult());
});
```

### `Union.Combine(...)` — aggregate two results

Run two independent operations and get both values together. Returns the first error encountered, or a combined success tuple:

```csharp
var nameResult  = ValidateName(command.Name);
var emailResult = ValidateEmail(command.Email);

var combined = Union.Combine(nameResult, emailResult);

if (!combined.IsSuccess(out var pair, out var err))
    return Union.Fail<User>(err!);

// pair.First = validated name, pair.Second = validated email
var user = new User(pair.First, pair.Second);
```

### `UnionWrapper.RunAsync` — legacy bridge

Wraps existing exception-throwing code into a union without rewriting the underlying service. Catches common exceptions and maps them to the appropriate `UnionError`:

| Exception | Mapped to |
|---|---|
| `null` return value | `UnionError.NotFound("Result")` |
| `UnauthorizedAccessException` | `UnionError.Unauthorized()` |
| `KeyNotFoundException` | `UnionError.NotFound(message)` |
| `OperationCanceledException` | re-thrown (propagates cancellation) |
| Any other `Exception` | `UnionError.SystemFailure(ex)` |

```csharp
// Before — legacy code throws
var user = await legacyRepo.GetUserAsync(id);  // may throw KeyNotFoundException

// After — wrapped in a union; no try/catch at the call site
var result = await UnionWrapper.RunAsync(() => legacyRepo.GetUserAsync(id));

if (!result.IsSuccess(out var user, out var err))
    return err!.ToHttpResult();

Console.WriteLine(user!.Name);
```

---

## Adapters

### UnionRailway.AspNetCore — HTTP Results & ProblemDetails

Translates any `(T Value, UnionError? Error)` tuple or bare `UnionError` into an `IResult` for Minimal APIs or controller action helpers. All error responses follow **RFC 7807 Problem Details**:

| `UnionError` subtype | HTTP status |
|---|---|
| `NotFound` | `404 Not Found` |
| `Conflict` | `409 Conflict` |
| `Unauthorized` | `401 Unauthorized` |
| `Forbidden` | `403 Forbidden` |
| `Validation` | `400 Bad Request` (with `errors` field map) |
| `SystemFailure` | `500 Internal Server Error` |

```csharp
// GET /products/{id} — returns 200 or appropriate error
app.MapGet("/products/{id:int}", async (int id, ProductService svc) =>
{
    var result = await svc.GetByIdAsync(id);
    return result.ToHttpResult();
    // Union.Ok(product)       → 200 { ... }
    // NotFound error          → 404 { "title": "Not Found", "detail": "..." }
    // SystemFailure error     → 500 { "title": "Internal Server Error" }
});

// POST /products — returns 201 Created with Location header on success
app.MapPost("/products", async (CreateProductRequest req, ProductService svc) =>
{
    var result = await svc.CreateAsync(req.Name, req.Sku, req.Price, req.Stock);
    return result.ToHttpResult(createdUri: result.Error is null
        ? $"/products/{result.Value.Id}"
        : null);
    // Union.Ok(product)       → 201 Created  Location: /products/42
    // Validation error        → 400 { "errors": { "Sku": ["Required"] } }
    // Conflict error          → 409 { "detail": "SKU already exists" }
});

// DELETE — translate a bare UnionError
app.MapDelete("/products/{id:int}", async (int id, ProductService svc) =>
{
    var (_, err) = await svc.DeleteAsync(id);
    return err is not null ? err.ToHttpResult() : Results.NoContent();
});
```

**Validation 400 response body (RFC 7807):**

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email":    ["Must be a valid email address"],
    "Password": ["Minimum 8 characters"]
  }
}
```

---

### UnionRailway.EntityFrameworkCore — Smart EF Core Queries

Eliminates manual null checks and wraps database exceptions automatically.

**`FirstOrDefaultAsUnionAsync`** — returns `NotFound` when the entity doesn't exist, `SystemFailure` on database errors:

```csharp
// Before — manual null check + exception handling
var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
if (product is null)
    return Union.Fail<Product>(new UnionError.NotFound("Product"));

// After — one line
var (product, err) = await db.Products
    .FirstOrDefaultAsUnionAsync("Product", p => p.Id == id);

if (err is not null) return Union.Fail<Product>(err);
// product is safe here
```

**`SaveChangesAsUnionAsync`** — returns the affected row count, maps concurrency and unique-constraint violations to `Conflict`:

```csharp
db.Products.Add(newProduct);
var (rowsAffected, saveErr) = await db.SaveChangesAsUnionAsync();

if (saveErr is not null)
    return Union.Fail<Product>(saveErr);
    // Conflict  → 409 (duplicate SKU, optimistic concurrency)
    // SystemFailure → 500

logger.LogInformation("Saved {Rows} rows", rowsAffected);
```

| Exception | Mapped to |
|---|---|
| `DbUpdateConcurrencyException` | `Conflict` |
| `DbUpdateException` (unique constraint) | `Conflict` |
| `DbUpdateException` (other) | `SystemFailure` |
| Any other `Exception` | `SystemFailure` |

---

### UnionRailway.HttpClient — Automatic HTTP Error Mapping

Wraps `System.Net.Http.HttpClient` calls and maps HTTP status codes to typed union tuples. **400 responses are automatically parsed as RFC 7807 problem details** and surfaced as `UnionError.Validation` with the full field-error map.

```csharp
// GET
var (user, err) = await httpClient.GetFromJsonAsUnionAsync<UserDto>("/api/users/1");

if (err is not null)
{
    // 401 → UnionError.Unauthorized
    // 403 → UnionError.Forbidden("...")
    // 404 → UnionError.NotFound("...")
    // 409 → UnionError.Conflict("...")
    // 400 → UnionError.Validation({ "Email": ["Invalid"] })  ← parsed from RFC 7807 body
    // 5xx → UnionError.SystemFailure(...)
    return err.ToHttpResult();
}

// POST
var (created, createErr) = await httpClient.PostAsJsonAsUnionAsync<CreatedUserDto>(
    "/api/users",
    new { Name = "Alice", Email = "alice@example.com" });

// PUT
var (updated, updateErr) = await httpClient.PutAsJsonAsUnionAsync<UserDto>(
    $"/api/users/{id}",
    new { Name = "Alice Updated" });

// DELETE
var (deleted, deleteErr) = await httpClient.DeleteAsUnionAsync($"/api/users/{id}");
```

**Status code mapping:**

| HTTP Status | Union result |
|---|---|
| `200 / 201` | `Union.Ok<T>` (deserialized JSON body) |
| `400` | `UnionError.Validation` (RFC 7807 `errors` map parsed) |
| `401` | `UnionError.Unauthorized` |
| `403` | `UnionError.Forbidden` (detail from problem body) |
| `404` | `UnionError.NotFound` (detail from problem body) |
| `409` | `UnionError.Conflict` (detail from problem body) |
| other 4xx / 5xx | `UnionError.SystemFailure` (with status message) |
| Network / timeout | `UnionError.SystemFailure` (wrapped exception) |

---

## Real-World Example: Order Service

A complete example showing how the three adapters compose in a single service layer:

```csharp
// OrderService.cs
public sealed class OrderService(AppDbContext db, PaymentGatewayClient payments)
{
    public async ValueTask<(Order Value, UnionError? Error)> PlaceOrderAsync(
        CreateOrderRequest req, string cardToken, CancellationToken ct = default)
    {
        // ── Step 1: validate inputs ───────────────────────────────────────────
        var errs = new List<(string, string[])>();
        if (req.Quantity <= 0)
            errs.Add(("Quantity", ["Quantity must be at least 1"]));
        if (string.IsNullOrWhiteSpace(cardToken))
            errs.Add(("CardToken", ["Payment card token is required"]));

        if (errs.Count > 0)
            return Union.Fail<Order>(UnionError.CreateValidation(errs));

        // ── Step 2: look up the product ───────────────────────────────────────
        var (product, productErr) = await db.Products
            .FirstOrDefaultAsUnionAsync("Product", p => p.Id == req.ProductId, ct);

        if (productErr is not null) return Union.Fail<Order>(productErr);

        // ── Step 3: check and reserve stock ──────────────────────────────────
        if (product!.StockQty < req.Quantity)
            return Union.Fail<Order>(new UnionError.Conflict(
                $"Insufficient stock: requested {req.Quantity}, available {product.StockQty}"));

        product.StockQty -= req.Quantity;

        // ── Step 4: charge the card via external HTTP gateway ─────────────────
        var (payment, paymentErr) = await payments.ChargeAsync(
            new PaymentRequest(req.CustomerId, product.Price * req.Quantity, cardToken), ct);

        if (paymentErr is not null)
        {
            // Roll back reserved stock before propagating the payment error
            product.StockQty += req.Quantity;
            await db.SaveChangesAsync(ct);
            return Union.Fail<Order>(paymentErr);
        }

        // ── Step 5: persist the confirmed order ───────────────────────────────
        var order = new Order
        {
            CustomerId = req.CustomerId,
            ProductId  = req.ProductId,
            Quantity   = req.Quantity,
            TotalPrice = product.Price * req.Quantity,
            Status     = $"Confirmed (txn: {payment!.TransactionId})"
        };
        db.Orders.Add(order);

        var (_, saveErr) = await db.SaveChangesAsUnionAsync(ct);
        return saveErr is not null
            ? Union.Fail<Order>(saveErr)
            : Union.Ok(order);
    }
}
```

Wire it up in a Minimal API:

```csharp
app.MapPost("/orders", async (CreateOrderRequest req, string cardToken, OrderService svc) =>
{
    var result = await svc.PlaceOrderAsync(req, cardToken);

    return result.ToHttpResult(createdUri: result.Error is null
        ? $"/orders/{result.Value.Id}"
        : null);
    // 201 Created        — happy path
    // 400 Bad Request    — Validation (bad qty, missing token)
    // 404 Not Found      — product doesn't exist
    // 409 Conflict       — insufficient stock or declined payment
    // 500 Server Error   — DB or unexpected failure
});
```

---

## Error Taxonomy Reference

| Subtype | Constructor | Typical cause |
|---|---|---|
| `NotFound` | `new UnionError.NotFound(resource)` | DB query returned null; 404 from API |
| `Conflict` | `new UnionError.Conflict(reason)` | Duplicate key; optimistic concurrency; insufficient stock |
| `Unauthorized` | `new UnionError.Unauthorized()` | Missing or expired token |
| `Forbidden` | `new UnionError.Forbidden(reason)` | Insufficient permissions |
| `Validation` | `UnionError.CreateValidation([...])` | Input validation failures |
| `SystemFailure` | `new UnionError.SystemFailure(ex)` | Unhandled exception; network failure |

---

## Playground

The `tests/UnionRailway.Playground` project is a self-contained console application that demonstrates every feature of the library through a realistic **e-commerce back-end** simulation:

- `ProductService.cs` — EF Core adapter usage: `FirstOrDefaultAsUnionAsync`, `SaveChangesAsUnionAsync`, duplicate-SKU conflict detection
- `PaymentGatewayClient.cs` — HttpClient adapter usage: `PostAsJsonAsUnionAsync` to an external payment gateway
- `OrderService.cs` — full railway pipeline: validate → find product → deduct stock → charge card → persist; stock roll-back on payment failure
- `Program.cs` — 11 scenarios covering success, validation errors, stock conflicts, declined cards, blocked cards, and not-found lookups

```bash
dotnet run --project tests/UnionRailway.Playground
```

---

## Testing

The union tuple type is trivially testable — it's a plain struct, no mocking needed:

```csharp
// Assert success
var result = await svc.GetByIdAsync(existingId);
Assert.Null(result.Error);
Assert.Equal("Widget Pro", result.Value.Name);

// Assert specific error subtype
var (_, err) = await svc.GetByIdAsync(unknownId);
var notFound = Assert.IsType<UnionError.NotFound>(err);
Assert.Equal("Product", notFound.Resource);

// Assert validation errors
var (_, validationErr) = await svc.CreateAsync(name: "", sku: "X", price: -1m, stock: 0);
var validation = Assert.IsType<UnionError.Validation>(validationErr);
Assert.Contains("Name", validation.Fields.Keys);
Assert.Contains("Price", validation.Fields.Keys);

// Unwrap in arrange phase (throws clearly on unexpected failure)
var product = (await svc.GetByIdAsync(knownId)).Unwrap();
```

---

## License

MIT © 2025 — see [LICENSE](LICENSE) for details.
