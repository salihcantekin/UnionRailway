# UnionRailway

[![NuGet](https://img.shields.io/nuget/v/UnionRailway.svg)](https://www.nuget.org/packages/UnionRailway)
[![Build Status](https://img.shields.io/github/actions/workflow/status/salihcantekin/UnionRailway/dotnet.yml?branch=main)](https://github.com/salihcantekin/UnionRailway/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**No jargon. No ceremony. Just clean error handling with early returns.**

UnionRailway is a C# library that replaces `try-catch` and thrown exceptions with simple, type-safe `Result<T>` return types. Check success, grab the data or error, and move on.

---

## Why UnionRailway?

Traditional .NET error handling has real problems:

- **Hidden failures.** A method signature says `User GetUser(int id)` but it might throw five different exceptions. The caller has no idea.
- **Scattered logic.** `try-catch` blocks obscure your domain logic and make code harder to read.
- **Performance cost.** Throwing exceptions is expensive in hot paths.

**UnionRailway fixes this.** Every operation returns a `Result<T>` that is either `Ok(data)` or `Error(error)`. You check it, handle it, and move on. No hidden surprises.

```csharp
if (!result.IsSuccess(out var user, out var error))
    return error; // early return - done

// use user safely here
```

That's it. No functional programming jargon. No learning curve. Just C# you already know.

---

## Installation

```bash
# Core library
dotnet add package UnionRailway

# ASP.NET Core adapter (Minimal APIs / Controllers)
dotnet add package UnionRailway.AspNetCore

# Entity Framework Core adapter
dotnet add package UnionRailway.EntityFrameworkCore

# HttpClient adapter
dotnet add package UnionRailway.HttpClient
```

---

## Quick Start: The Early Return Pattern

The core idea is simple: check if the result succeeded, use `out` parameters to get the data or the error, and return early if it failed.

```csharp
using UnionRailway;

Result<User> result = await userService.GetByIdAsync(42);

if (!result.IsSuccess(out var user, out var error))
{
    // error is a UnionError - handle it however you want
    logger.LogWarning("Failed: {Error}", error);
    return;
}

// user is guaranteed to have a value here
Console.WriteLine($"Hello, {user.Name}!");
```

### Chaining Multiple Operations

```csharp
var userResult = await userService.GetByIdAsync(userId);
if (!userResult.IsSuccess(out var user, out var userError))
    return userError;

var orderResult = await orderService.GetLatestAsync(user.Id);
if (!orderResult.IsSuccess(out var order, out var orderError))
    return orderError;

// Both succeeded - use user and order safely
Console.WriteLine($"{user.Name}'s latest order: {order.Id}");
```

---

## ASP.NET Core: One-Line HTTP Responses

The `UnionRailway.AspNetCore` adapter converts any `Result<T>` into the correct HTTP response with a single call to `.ToHttpResult()`.

```csharp
using UnionRailway;
using UnionRailway.AspNetCore;

app.MapGet("/users/{id}", async (int id, UserService service) =>
{
    var result = await service.GetByIdAsync(id);
    return result.ToHttpResult(); // That's it. One line.
});
```

**What `.ToHttpResult()` does automatically:**

| Result | HTTP Response |
|---|---|
| `Ok(data)` | `200 OK` with the data |
| `NotFound("User")` | `404 Not Found` |
| `Conflict("Duplicate")` | `409 Conflict` |
| `Unauthorized` | `401 Unauthorized` |
| `Validation(fields)` | `400 Bad Request` with validation errors |
| `SystemFailure(ex)` | `500 Problem Details` |

### Early Return + ToHttpResult

For more complex endpoints, combine early returns with `.ToHttpResult()`:

```csharp
app.MapPost("/orders", async (CreateOrderRequest req, OrderService service, UserService users) =>
{
    var userResult = await users.GetByIdAsync(req.UserId);
    if (!userResult.IsSuccess(out var user, out var error))
        return error.ToHttpResult(); // returns 404, 401, etc.

    var orderResult = await service.CreateAsync(user, req);
    return orderResult.ToHttpResult(); // returns 200 or error
});
```

---

## Entity Framework Core Adapter

Wraps database operations so they return `Result<T>` instead of throwing exceptions.

```csharp
using UnionRailway.EntityFrameworkCore;

// Find by primary key - returns NotFound instead of null
var result = await dbContext.FindAsUnionAsync<User>(userId);

// Query - returns NotFound instead of null
var result = await dbContext.Users
    .Where(u => u.Email == email)
    .FirstOrDefaultAsUnionAsync();

// Save changes - catches DbUpdateException automatically
dbContext.Users.Add(newUser);
var saveResult = await dbContext.SaveChangesAsUnionAsync();

if (!saveResult.IsSuccess(out var count, out var error))
{
    // error is Conflict (concurrency) or SystemFailure (other DB errors)
    logger.LogError("Save failed: {Error}", error);
}
```

| Scenario | Mapped To |
|---|---|
| Entity not found (null) | `NotFound` |
| `DbUpdateConcurrencyException` | `Conflict` |
| `DbUpdateException` | `SystemFailure` |
| Any other exception | `SystemFailure` |

---

## HttpClient Adapter

Maps HTTP responses to `Result<T>` based on status codes.

```csharp
using UnionRailway.HttpClient;

var result = await httpClient.GetAsUnionAsync<WeatherForecast>("api/weather/istanbul");

if (!result.IsSuccess(out var forecast, out var error))
{
    // error already mapped from HTTP status code
    Console.WriteLine($"API call failed: {error}");
    return;
}

Console.WriteLine($"Temperature: {forecast.Temperature}");
```

| HTTP Status | Mapped To |
|---|---|
| 200 / 201 / 202 | `Ok(data)` |
| 204 No Content | `Ok(default)` |
| 400 Bad Request | `Validation` |
| 401 / 403 | `Unauthorized` |
| 404 | `NotFound` |
| 409 | `Conflict` |
| 5xx / Other | `SystemFailure` |

---

## Advanced: Exhaustive Error Handling with Switch

When you need to handle each error type differently, use standard C# `switch` pattern matching:

```csharp
var result = await userService.GetByIdAsync(id);

var response = result switch
{
    Result<User>.Ok(var user) => $"Found: {user.Name}",
    Result<User>.Error(UnionError.NotFound(var resource)) => $"{resource} not found",
    Result<User>.Error(UnionError.Conflict(var reason)) => $"Conflict: {reason}",
    Result<User>.Error(UnionError.Unauthorized) => "Access denied",
    Result<User>.Error(UnionError.Validation(var fields)) =>
        $"Invalid: {string.Join(", ", fields.Values)}",
    Result<User>.Error(UnionError.SystemFailure(var ex)) => $"Error: {ex.Message}",
    _ => "Unknown"
};
```

---

## Error Types

UnionRailway provides five built-in error types:

```csharp
public abstract record UnionError
{
    public sealed record NotFound(string Resource) : UnionError;
    public sealed record Conflict(string Reason) : UnionError;
    public sealed record Unauthorized() : UnionError;
    public sealed record Validation(IReadOnlyDictionary<string, string> Fields) : UnionError;
    public sealed record SystemFailure(Exception Ex) : UnionError;
}
```

---

## Target Frameworks

| Package | Targets |
|---|---|
| `UnionRailway` | .NET Standard 2.1, .NET 8, .NET 9 |
| `UnionRailway.AspNetCore` | .NET 8, .NET 9 |
| `UnionRailway.EntityFrameworkCore` | .NET 8, .NET 9 |
| `UnionRailway.HttpClient` | .NET Standard 2.1, .NET 8, .NET 9 |

---

## License

This project is licensed under the [MIT License](LICENSE).
