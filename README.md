# UnionRailway

<div align="center">

[![Build](https://img.shields.io/github/actions/workflow/status/salihcantekin/UnionRailway/ci.yml?branch=main&label=build)](https://github.com/salihcantekin/UnionRailway)
[![NuGet](https://img.shields.io/nuget/v/UnionRailway.svg)](https://www.nuget.org/packages/UnionRailway)
[![Downloads](https://img.shields.io/nuget/dt/UnionRailway?label=downloads&color=blue)](https://www.nuget.org/packages/UnionRailway)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2011.0+-purple)](https://dotnet.microsoft.com)

**Type-safe error handling for C# that actually feels native.**

[Quick Start](#-quick-start-2-minutes) · [Why This?](#-why-unionrailway) · [Benchmarks](#-performance) · [Full Docs](#-complete-guide)

</div>

---

## 🎯 The Problem

**Your error handling probably looks like this:**

```csharp
public async Task<IResult> GetUser(int id)
{
    try
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
            return Results.NotFound();

        return Results.Ok(user);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to get user");
        return Results.Problem("Something went wrong");
    }
}
```

**Problems:**
- ❌ Exceptions for control flow (slow, unclear)
- ❌ No type safety (caller doesn't know what errors to expect)
- ❌ Inconsistent error responses across APIs
- ❌ Manual mapping everywhere

---

## ✨ The Solution

**With UnionRailway, it becomes:**

```csharp
public async ValueTask<Rail<User>> GetUserAsync(int id)
{
    return await db.Users.FirstOrDefaultAsUnionAsync("User", x => x.Id == id);
}

// In your endpoint:
app.MapGet("/users/{id:int}", async (int id, UserService service) =>
{
    var result = await service.GetUserAsync(id);
    return result.ToHttpResult(); // ✅ Automatic RFC 7807 Problem Details
});
```

**Benefits:**
- ✅ **Type-safe** - Compiler knows all possible errors
- ✅ **Zero boilerplate** - One line to return RFC 7807 responses
- ✅ **Fast** - 0.5ns success path, zero allocations
- ✅ **Consistent** - Same error vocabulary everywhere (DB → Service → HTTP)

---

## 🚀 Quick Start (2 Minutes)

### 1. Install

```bash
dotnet add package UnionRailway
dotnet add package UnionRailway.AspNetCore
dotnet add package UnionRailway.EntityFrameworkCore  # Optional
```

### 2. Return `Rail<T>` from your services

```csharp
public class UserService
{
    public async ValueTask<Rail<User>> GetUserAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
            return new UnionError.NotFound("User");

        return user; // ✅ Implicit conversion
    }
}
```

### 3. Convert to HTTP responses

```csharp
app.MapGet("/users/{id}", async (int id, UserService service) =>
    (await service.GetUserAsync(id)).ToHttpResult());
```

**Or even simpler with the endpoint filter — no `.ToHttpResult()` needed:**

```csharp
var api = app.MapGroup("/api").WithRailwayFilter();

api.MapGet("/users/{id}", async (int id, UserService svc) =>
    await svc.GetUserAsync(id));  // Returns Rail<User> directly!
```

**That's it!** You now have:
- ✅ Automatic 404 for NotFound
- ✅ RFC 7807 Problem Details
- ✅ OpenAPI documentation
- ✅ Type-safe error handling

---

## 🎭 Why UnionRailway?

### **vs. Exceptions**
```csharp
// ❌ Exceptions: Slow, unclear, unsafe
try { var user = await repo.GetAsync(id); }
catch (NotFoundException) { return Results.NotFound(); }
catch (UnauthorizedException) { return Results.Unauthorized(); }
// What other exceptions can this throw? 🤷

// ✅ UnionRailway: Fast, explicit, type-safe
var result = await repo.GetAsync(id);
return result.ToHttpResult();
// Compiler knows: Success | NotFound | Unauthorized ✅
```

### **vs. ErrorOr / LanguageExt**
```csharp
// ❌ Other libraries: Manual mapping, no ecosystem
ErrorOr<User> result = await repo.GetAsync(id);
return result.Match(
    value => Results.Ok(value),
    errors => errors[0].Type switch {
        ErrorType.NotFound => Results.NotFound(),
        ErrorType.Unauthorized => Results.Unauthorized(),
        _ => Results.Problem() // 😰 Manual everywhere
    });

// ✅ UnionRailway: Automatic RFC 7807, built-in integrations
var result = await repo.GetAsync(id);
return result.ToHttpResult(); // ✨ Done!
```

**[📊 Full comparison with LanguageExt, ErrorOr, OneOf, FluentResults →](docs/COMPARISON.md)**

---

## 🎁 What You Get

### 1. **Semantic Error Types** (not strings)
```csharp
UnionError error = result.Error.GetValueOrDefault();

var message = error.Value switch
{
    UnionError.NotFound nf      => $"Missing: {nf.Resource}",
    UnionError.Conflict c       => $"Conflict: {c.Reason}",
    UnionError.Unauthorized     => "Authentication required",
    UnionError.Forbidden f      => $"Forbidden: {f.Reason}",
    UnionError.Validation v     => $"{v.Fields.Count} validation errors",
    UnionError.SystemFailure sf => sf.Ex.Message,
    UnionError.Custom c         => $"{c.Code}: {c.Message}",
    _                           => "Unknown"
};
```

#### **Custom Domain Errors**

Don't let the predefined categories limit you. `UnionError.Custom` lets you
represent application-specific error conditions with a machine-readable code,
custom HTTP status, and optional metadata:

```csharp
// Return a domain-specific error
return new UnionError.Custom(
    Code: "RATE_LIMIT_EXCEEDED",
    Message: "Too many requests, please retry later.",
    StatusCode: 429,
    Extensions: new Dictionary<string, object>
    {
        ["retryAfter"] = 30,
        ["limit"] = 100
    });

// Maps to ProblemDetails with errorCode extension and your status code
```

### 2. **Railway Composition**
```csharp
var result = await GetUserAsync(id)
    .BindAsync(user => GetOrdersAsync(user.Id))
    .MapAsync(orders => new OrderSummary(orders))
    .ToHttpResultAsync();
```

#### **Working with Anonymous Types & Object Results**

When returning `object` (e.g., anonymous types for DTOs), use `BindObject` to avoid generic type inference issues:

```csharp
// ✅ GOOD: Use BindObject for object/anonymous type results
var result = await GetProductAsync(id)
    .BindObject(
        product => product.Stock > 0,
        product => new { product.Id, product.Name, Status = "In Stock" },
        product => new UnionError.Conflict("Out of stock"));

// ✅ GOOD: Classic style also works
var result = await GetProductAsync(id)
    .BindObject(product => product.Stock > 0
        ? Union.Ok(new { product.Id, product.Name })  // Union.Ok(object) overload
        : Union.Fail<object>(new UnionError.Conflict("Out of stock")));

// ❌ AVOID: Generic inference issues with Bind<T, object>
var result = await GetProductAsync(id)
    .Bind(product => Union.Ok(new { product.Id }));  // May cause type issues!
```

**Why `BindObject`?**
- Prevents C# generic type inference issues with anonymous types
- Predicate-style syntax is more readable for conditional logic
- Explicitly signals that boxing to `object` will occur

**When to use what:**
- Use `Map` when transforming the success value (no error path)
- Use `Bind` when chaining operations that return `Rail<T>` (with explicit types)
- Use `BindObject` when returning `object` or anonymous types with conditional errors

#### **Recovery & Side Effects**

```csharp
// Recover from specific errors with a fallback
var user = await GetUserAsync(id)
    .RecoverAsync<User, UnionError.NotFound>(_ => guestUser);

// Execute side effects without changing the value
var result = await GetUserAsync(id)
    .TapAsync(user => LogAccessAsync(user.Id))
    .ToHttpResultAsync();
```

### 3. **Ecosystem Integration**

| Package | What It Does |
|---------|--------------|
| `UnionRailway` | Core `Rail<T>` and `UnionError` types |
| `UnionRailway.AspNetCore` | RFC 7807 Problem Details mapping |
| `UnionRailway.AspNetCore.OpenApi` | Automatic Swagger/OpenAPI docs |
| `UnionRailway.EntityFrameworkCore` | `FirstOrDefaultAsUnionAsync` helpers |
| `UnionRailway.HttpClient` | HTTP status → `Rail<T>` conversion |

### 4. **Migration-Friendly**
```csharp
// Wrap legacy exception-based code:
var result = await UnionWrapper.RunAsync(() => legacyService.LoadAsync());

// Wrap nullable returns:
var maybeUser = await UnionWrapper.RunNullableAsync(() => repo.FindAsync(id));
```

---

## ⚡ Performance

**Real benchmarks (BenchmarkDotNet, .NET 8):**

| Operation | Time | Allocated | Notes |
|-----------|------|-----------|-------|
| Create success | **0.5 ns** | **0 B** | Stack-only, zero allocation |
| Create failure | 2.8 ns | 24 B | Error record allocation |
| Create Custom error | ~3.5 ns | 32 B | Record with optional extensions |
| Map operation | 1.3 ns | 0 B | AggressiveInlining |
| Bind operation | 36 ns | 40 B | Function call overhead |
| Recover (matching) | ~2 ns | 0 B | Single `is` type check |
| Recover (non-matching) | ~1 ns | 0 B | Short-circuit, no work |
| MapAsync | **48 ns** | 0 B | ValueTask overhead only |
| Service chain (3 ops) | 20 ns | 120 B | Real-world scenario |

**Why so fast?**
- ✅ Struct-based `Rail<T>` lives on stack
- ✅ Zero allocations on success path
- ✅ `ValueTask` for async (vs `Task`)
- ✅ AggressiveInlining on hot paths
- ✅ `configureProblem` callback is null-guarded — zero overhead when unused
- ✅ `Custom.Extensions` is `null` by default — pay only when you use it

**Run yourself:**
```bash
cd tests/UnionRailway.Benchmarks
dotnet run -c Release
```

---

## 📖 Complete Guide

### **`Rail<T>` - The Core Type**

Represents **exactly one** of:
- ✅ Success value of type `T`
- ❌ `UnionError`

```csharp
public ValueTask<Rail<User>> GetUserAsync(int id)
{
    // Option 1: Implicit conversion
    return user;

    // Option 2: Explicit creation
    return Union.Ok(user);
    return Union.Fail<User>(new UnionError.NotFound("User"));
}
```

### **Pattern Matching**

```csharp
// Style 1: IsSuccess pattern
if (!result.IsSuccess(out var user, out var error))
    return error.GetValueOrDefault().ToHttpResult();

// Style 2: Match
return result.Match(
    onOk: user => Results.Ok(user),
    onError: error => error.ToHttpResult());

// Style 3: Error property check
if (result.Error is not null)
    return result.Error.GetValueOrDefault().ToHttpResult();
```

### **Railway Composition**

```csharp
// Sync
var result = Union.Ok(5)
    .Map(x => x * 2)           // Transform success
    .Bind(x => ValidateAsync(x)); // Chain operations

// Async
var result = await GetUserAsync(id)
    .BindAsync(user => GetOrdersAsync(user.Id))
    .MapAsync(orders => orders.Count)
    .ToHttpResultAsync();
```

### **ASP.NET Core Integration**

```csharp
app.MapGet("/users/{id:int}", async (int id, UserService service) =>
    (await service.GetUserAsync(id)).ToHttpResult());

app.MapPost("/users", async (CreateUserRequest req, UserService service) =>
    (await service.CreateAsync(req)).ToHttpResult(createdUri: $"/users/{id}"))
    .WithCreatedRailOpenApi<RouteHandlerBuilder, UserDto>();

// Rail<Unit> automatically returns 204 No Content
app.MapDelete("/users/{id}", async (int id, UserService service) =>
    (await service.DeleteAsync(id)).ToHttpResult());
```

#### **Customizing ProblemDetails**

Post-process any `ProblemDetails` response to add trace IDs, strip details
in production, or enrich extensions:

```csharp
return result.ToHttpResult(configureProblem: pd =>
{
    pd.Extensions["traceId"] = Activity.Current?.Id;
    if (env.IsProduction())
        pd.Detail = "An error occurred.";
});
```

#### **Custom Error Mapping (`IUnionErrorMapper`)**

For full control over how errors translate to HTTP responses, implement
`IUnionErrorMapper` and register it in DI. When `TryMap` returns non-null,
that result is used directly; when it returns `null`, the default RFC 7807
mapping kicks in:

```csharp
public class CustomErrorMapper : IUnionErrorMapper
{
    public IResult? TryMap(UnionError error) => error.Value switch
    {
        UnionError.NotFound nf => Results.Problem(
            detail: $"We could not locate '{nf.Resource}'.",
            statusCode: 404,
            title: "Resource Not Found"),
        UnionError.Custom { Code: "RATE_LIMIT" } c => Results.Problem(
            detail: c.Message,
            statusCode: 429,
            title: "Rate Limited"),
        _ => null // fall back to default mapping
    };
}

// Registration:
builder.Services.AddSingleton<IUnionErrorMapper, CustomErrorMapper>();

// Usage (inject via DI or pass directly):
return result.ToHttpResult(errorMapper: mapper);
```

#### **Zero-Boilerplate with `RailEndpointFilter`**

Instead of calling `.ToHttpResult()` on every endpoint, add the filter once
and return `Rail<T>` directly from your handlers:

```csharp
// Option 1: Per-group (recommended)
var api = app.MapGroup("/api").WithRailwayFilter();

api.MapGet("/users/{id}", async (int id, UserService svc) =>
    await svc.GetUserAsync(id));  // Rail<User> → 200/404 automatically

api.MapDelete("/users/{id}", async (int id, UserService svc) =>
    await svc.DeleteAsync(id));   // Rail<Unit> → 204 automatically

// Option 2: Per-endpoint
app.MapGet("/users/{id}", handler).WithRailwayFilter();
```

The filter resolves `IUnionErrorMapper` and `RailwayOptions` from DI automatically.

#### **Global Exception Handling**

Catch unhandled exceptions across all endpoints and return consistent
RFC 7807 responses — matching UnionRailway's error format:

```csharp
var app = builder.Build();
app.UseRailwayExceptionHandler(); // Add early in pipeline
app.UseAuthentication();
app.UseAuthorization();
```

#### **`AddRailway()` — One-line DI Setup**

Register all UnionRailway services with a single call:

```csharp
builder.Services.AddRailway(options =>
{
    // Global ProblemDetails enrichment (applied to ALL error responses)
    options.ConfigureProblem = pd =>
        pd.Extensions["traceId"] = Activity.Current?.Id;
});

// Or with a custom mapper:
builder.Services.AddRailway<CustomErrorMapper>(options => ...);
```

### **Entity Framework Core**

```csharp
// FirstOrDefault → Rail<T>
var user = await db.Users.FirstOrDefaultAsUnionAsync("User", x => x.Id == id);

// SaveChanges → Rail<int>
var result = await db.SaveChangesAsUnionAsync();
```

### **HttpClient**

```csharp
var result = await httpClient.GetFromJsonAsUnionAsync<UserDto>("/users/42");
// Automatic mapping: 2xx → Success, 4xx/5xx → Error
```

---

## 🔮 Future: Native C# Unions

UnionRailway is architected for **seamless migration** to native C# unions when .NET 11 stable is released:

**Current Implementation (.NET 8 & 11 Preview):**
```csharp
[Union]  // High-performance struct-based implementation
public readonly struct Rail<T> { /* ... */ }
public readonly struct UnionError { /* ... */ }
```

**Future (.NET 11 Stable Release):**
```csharp
public union Rail<T>(T, UnionError);      // Native union types
public union UnionError(...);             // Native discriminated union
```

**Your Application Code:** Zero changes required! 🎉

The library is designed to automatically switch to native unions when:
- .NET 11 stable is released with finalized union support
- The union feature is production-ready (expected in .NET 11 RTM)

Until then, UnionRailway uses a highly optimized struct-based implementation that provides:
- ✅ 0.5ns success path performance
- ✅ Zero heap allocations
- ✅ Full type safety
- ✅ Compatible API surface with future native unions

> **Note:** Union types are currently available in .NET 11 preview builds but are not yet enabled in this library until the feature stabilizes in the final release.

**Target Frameworks:** .NET 8.0, .NET 11.0 (and future versions)

---

## 💡 Best Practices & Common Pitfalls

### ✅ Do's

**1. Use `BindObject` for Anonymous Types / `object` Results**
```csharp
// ✅ GOOD: Type-safe and clear
.BindObject(
    product => product.Stock > 0,
    product => new { product.Id, product.Name, Status = "Available" },
    product => new UnionError.Conflict("Out of stock"))
```

**2. Use `Union.Ok(object)` When Boxing to Object**
```csharp
// ✅ GOOD: Explicit object overload prevents inference issues
Rail<object> result = Union.Ok(new { Id = 1, Name = "Test" });
```

**3. Explicit Generic Parameters When Mixing Types**
```csharp
// ✅ GOOD: Clear and type-safe
.Bind<Product, OrderDto>(product => CreateOrderAsync(product))
```

**4. Use `ValueTask<Rail<T>>` for Async Methods**
```csharp
// ✅ GOOD: Zero allocation on cached results
public async ValueTask<Rail<User>> GetUserAsync(int id)
{
    var user = await db.Users.FindAsync(id);
    return user ?? new UnionError.NotFound("User");
}
```

**5. Chain Operations for Readability**
```csharp
// ✅ GOOD: Railway-style composition
var result = await GetProductAsync(id)
    .BindAsync(product => ValidateStockAsync(product))
    .MapAsync(product => new ProductDto(product))
    .ToHttpResultAsync();
```

### ❌ Don'ts

**1. Don't Use Generic `Bind` with Anonymous Types**
```csharp
// ❌ BAD: Generic type inference issues!
.Bind(p => Union.Ok(new { p.Id, p.Name }))
// May serialize as Rail wrapper instead of the object!

// ✅ GOOD: Use BindObject instead
.BindObject(p => new { p.Id, p.Name })
```

**2. Don't Mix Error Handling Styles**
```csharp
// ❌ BAD: Mixing exceptions with Railway
public async ValueTask<Rail<User>> GetUserAsync(int id)
{
    try
    {
        var user = await db.Users.FindAsync(id);
        return user ?? throw new NotFoundException(); // ❌ Don't throw!
    }
    catch (Exception ex)
    {
        return new UnionError.SystemFailure(ex);
    }
}

// ✅ GOOD: Pure Railway style
public async ValueTask<Rail<User>> GetUserAsync(int id)
{
    var user = await db.Users.FindAsync(id);
    return user ?? new UnionError.NotFound("User");
}
```

**3. Don't Ignore Error Cases**
```csharp
// ❌ BAD: Assuming success without checking
var user = result.Unwrap(); // May throw UnwrapException!

// ✅ GOOD: Check first or use Match
if (result.IsSuccess(out var user, out var error))
{
    // use user
}
else
{
    // handle error
}

// OR use Match
var message = result.Match(
    onSuccess: user => $"Hello {user.Name}",
    onError: error => $"Error: {error}");
```

**4. Don't Forget to `await` Async Chains**
```csharp
// ❌ BAD: Returning Task<Rail<T>> instead of Rail<T>
public async Task<IResult> GetUser(int id, UserService svc)
{
    var result = svc.GetUserAsync(id); // Missing await!
    return result.ToHttpResult(); // Won't compile!
}

// ✅ GOOD
public async Task<IResult> GetUser(int id, UserService svc)
{
    var result = await svc.GetUserAsync(id);
    return result.ToHttpResult();
}

// ✅ EVEN BETTER: Use ToHttpResultAsync
public async Task<IResult> GetUser(int id, UserService svc)
{
    return await svc.GetUserAsync(id).ToHttpResultAsync();
}
```

**5. Don't Over-Use `object` as Return Type**
```csharp
// ❌ BAD: Loses type safety
public ValueTask<Rail<object>> GetUserAsync(int id)
{
    return db.Users.FirstOrDefaultAsUnionAsync("User", x => x.Id == id)
        .MapAsync(user => (object)user); // Why box?
}

// ✅ GOOD: Keep strong typing as long as possible
public ValueTask<Rail<User>> GetUserAsync(int id)
{
    return db.Users.FirstOrDefaultAsUnionAsync("User", x => x.Id == id);
}

// Use object only at HTTP boundary for DTOs:
.MapAsync(user => new { user.Id, user.Name })
.ToHttpResultAsync();
```

### 🎯 Performance Tips

- ✅ Use `ValueTask<Rail<T>>` instead of `Task<Rail<T>>` for hot paths
- ✅ Prefer `MapAsync` / `BindAsync` over synchronous versions when awaiting
- ✅ Use `Rail<Unit>` for void-like operations (0 allocation)
- ✅ Avoid boxing to `object` unless necessary (DTOs at HTTP layer)
- ✅ Use `AggressiveInlining` extension methods for custom operators

---

## 🎓 Learn More

- **[Comparison with other libraries →](docs/COMPARISON.md)**  
  Why choose UnionRailway over LanguageExt, ErrorOr, OneOf, FluentResults

- **[Contributing →](CONTRIBUTING.md)**  
  How to contribute, coding conventions, PR process

- **[Changelog →](CHANGELOG.md)**  
  Version history and breaking changes

---

## 📦 Packages

| Package | Description | Version | Downloads |
|---------|-------------|---------|-----------|
| **UnionRailway** | Core types and railway operators | [![NuGet](https://img.shields.io/nuget/v/UnionRailway.svg)](https://www.nuget.org/packages/UnionRailway) | [![Downloads](https://img.shields.io/nuget/dt/UnionRailway?color=blue)](https://www.nuget.org/packages/UnionRailway) |
| **UnionRailway.AspNetCore** | RFC 7807 Problem Details mapping | [![NuGet](https://img.shields.io/nuget/v/UnionRailway.AspNetCore.svg)](https://www.nuget.org/packages/UnionRailway.AspNetCore) | [![Downloads](https://img.shields.io/nuget/dt/UnionRailway.AspNetCore?color=blue)](https://www.nuget.org/packages/UnionRailway.AspNetCore) |
| **UnionRailway.AspNetCore.OpenApi** | Swagger/OpenAPI metadata | [![NuGet](https://img.shields.io/nuget/v/UnionRailway.AspNetCore.OpenApi.svg)](https://www.nuget.org/packages/UnionRailway.AspNetCore.OpenApi) | [![Downloads](https://img.shields.io/nuget/dt/UnionRailway.AspNetCore.OpenApi?color=blue)](https://www.nuget.org/packages/UnionRailway.AspNetCore.OpenApi) |
| **UnionRailway.EntityFrameworkCore** | EF Core extensions | [![NuGet](https://img.shields.io/nuget/v/UnionRailway.EntityFrameworkCore.svg)](https://www.nuget.org/packages/UnionRailway.EntityFrameworkCore) | [![Downloads](https://img.shields.io/nuget/dt/UnionRailway.EntityFrameworkCore?color=blue)](https://www.nuget.org/packages/UnionRailway.EntityFrameworkCore) |
| **UnionRailway.HttpClient** | HttpClient extensions | [![NuGet](https://img.shields.io/nuget/v/UnionRailway.HttpClient.svg)](https://www.nuget.org/packages/UnionRailway.HttpClient) | [![Downloads](https://img.shields.io/nuget/dt/UnionRailway.HttpClient?color=blue)](https://www.nuget.org/packages/UnionRailway.HttpClient) |

---

## ❤️ Show Your Support

If UnionRailway helps your project, consider:
- ⭐ **Star this repo** on GitHub
- 📢 **Share** with your team
- 🐛 **Report issues** or suggest features
- 🤝 **Contribute** - PRs welcome!

---

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

---

<div align="center">

**Built with ❤️ for the C# community**

[Get Started](#-quick-start-2-minutes) · [Documentation](#-complete-guide) · [GitHub](https://github.com/salihcantekin/UnionRailway)

</div>

