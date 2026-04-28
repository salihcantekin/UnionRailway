# UnionRailway

<div align="center">

[![Build](https://img.shields.io/github/actions/workflow/status/salihcantekin/UnionRailway/ci.yml?branch=main&label=build)](https://github.com/salihcantekin/UnionRailway)
[![NuGet](https://img.shields.io/nuget/v/UnionRailway.svg)](https://www.nuget.org/packages/UnionRailway)
[![Downloads](https://img.shields.io/nuget/dt/UnionRailway?label=downloads&color=blue)](https://www.nuget.org/packages/UnionRailway)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)

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
    _                           => "Unknown"
};
```

### 2. **Railway Composition**
```csharp
var result = await GetUserAsync(id)
    .BindAsync(user => GetOrdersAsync(user.Id))
    .MapAsync(orders => new OrderSummary(orders))
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
| Map operation | 1.3 ns | 0 B | AggressiveInlining |
| Bind operation | 36 ns | 40 B | Function call overhead |
| MapAsync | **48 ns** | 0 B | ValueTask overhead only |
| Service chain (3 ops) | 20 ns | 120 B | Real-world scenario |

**Why so fast?**
- ✅ Struct-based `Rail<T>` lives on stack
- ✅ Zero allocations on success path
- ✅ `ValueTask` for async (vs `Task`)
- ✅ AggressiveInlining on hot paths

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

## 🔮 Future: .NET 11 Native Unions

UnionRailway is designed for **zero breaking changes** when C# gets native unions:

**Today (.NET 8):**
```csharp
[Union]  // Struct-based polyfill
public readonly struct Rail<T> { /* ... */ }
```

**Tomorrow (.NET 11):**
```csharp
public union Rail<T>(T, UnionError);  // Native!
```

**Your code:** No changes needed! 🎉

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

