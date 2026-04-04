# UnionRailway

[![NuGet](https://img.shields.io/nuget/v/UnionRailway.svg)](https://www.nuget.org/packages/UnionRailway)
[![Build Status](https://img.shields.io/github/actions/workflow/status/salihcantekin/UnionRailway/dotnet.yml?branch=main)](https://github.com/salihcantekin/UnionRailway/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Railway-Oriented Programming with C# Discriminated Unions. Eliminate `try-catch` blocks and replace exceptions with compile-time safe union return types.**

---

## Why UnionRailway?

Traditional .NET error handling relies on `try-catch` blocks and thrown exceptions. This approach has significant drawbacks:

- **Exceptions are invisible in method signatures.** Callers have no compile-time indication that a method can fail, leading to unhandled errors.
- **Performance overhead.** Exception throwing and catching is expensive, especially in hot paths.
- **Control flow obscured.** `try-catch` scatters error handling across the codebase, making domain logic harder to follow.

**UnionRailway** replaces this pattern with a functional, Railway-Oriented Programming (ROP) approach using C# discriminated unions (abstract record hierarchies with exhaustive pattern matching):

- Every operation returns a `Result<T>` that is either `Ok(T)` or `Error(UnionError)`.
- The compiler helps you handle every possible error case through pattern matching.
- No more hidden exceptions. Every failure path is explicit and type-safe.
- Functional composition with `Map`, `Bind`, `Match`, and `Tap` lets you chain operations cleanly.

---

## Installation

```bash
# Core library
dotnet add package UnionRailway

# Entity Framework Core adapter
dotnet add package UnionRailway.EntityFrameworkCore

# HttpClient adapter
dotnet add package UnionRailway.HttpClient
```

---

## Quick Start

### Core Usage

```csharp
using UnionRailway;

// Define a function that returns a Result
Result<User> GetUser(int id)
{
    var user = repository.FindById(id);
    return user is not null
        ? new Result<User>.Ok(user)
        : new Result<User>.Error(new UnionError.NotFound("User"));
}

// Chain operations with Map and Bind
var result = GetUser(42)
    .Map(user => user.Email)
    .Bind(email => ValidateEmail(email));
```

### Exhaustive Pattern Matching

The compiler enforces that you handle every variant of `UnionError`:

```csharp
var message = result switch
{
    Result<string>.Ok(var data) => $"Success: {data}",
    Result<string>.Error(UnionError.NotFound(var resource)) => $"{resource} was not found",
    Result<string>.Error(UnionError.Conflict(var reason)) => $"Conflict: {reason}",
    Result<string>.Error(UnionError.Unauthorized) => "Access denied",
    Result<string>.Error(UnionError.Validation(var fields)) => $"Validation failed: {string.Join(", ", fields.Keys)}",
    Result<string>.Error(UnionError.SystemFailure(var ex)) => $"System error: {ex.Message}",
    _ => "Unknown"
};
```

### Functional Composition

```csharp
// Map: Transform the success value
var nameResult = userResult.Map(user => user.Name);

// Bind: Chain to another Result-returning function
var orderResult = userResult.Bind(user => GetLatestOrder(user.Id));

// Tap: Side effects without changing the result
var logged = userResult.Tap(user => logger.LogInformation("Found user {Id}", user.Id));

// Match: Extract a final value from either branch
var response = userResult.Match(
    onOk: user => Ok(user),
    onError: err => MapToHttpResponse(err));

// Async variants
var asyncResult = await GetUserAsync(42)
    .MapAsync(user => user.Email)
    .BindAsync(email => ValidateEmailAsync(email));
```

---

## Entity Framework Core Adapter

The EF Core adapter wraps common database operations, automatically mapping exceptions and null results to the appropriate `UnionError` variant.

```csharp
using UnionRailway.EntityFrameworkCore;

// Query for a single entity (returns NotFound if null)
var result = await dbContext.Users
    .Where(u => u.Email == "alice@example.com")
    .FirstOrDefaultAsUnionAsync();

// Find by primary key
var found = await dbContext.FindAsUnionAsync<User>(userId);

// Save changes (catches DbUpdateException, concurrency violations, etc.)
dbContext.Users.Add(newUser);
var saveResult = await dbContext.SaveChangesAsUnionAsync();

// Pattern match the save result
var response = saveResult switch
{
    Result<int>.Ok(var count) => $"Saved {count} changes",
    Result<int>.Error(UnionError.Conflict(var reason)) => $"Concurrency conflict: {reason}",
    Result<int>.Error(UnionError.SystemFailure(var ex)) => $"Database error: {ex.Message}",
    _ => "Unexpected error"
};
```

### Automatic Error Mapping

| Scenario | Mapped To |
|---|---|
| Entity not found (null) | `UnionError.NotFound` |
| `DbUpdateConcurrencyException` | `UnionError.Conflict` |
| `DbUpdateException` | `UnionError.SystemFailure` |
| Any other exception | `UnionError.SystemFailure` |

---

## HttpClient Adapter

The HttpClient adapter maps HTTP status codes to `UnionError` variants, eliminating the need for manual status code checking.

```csharp
using UnionRailway.HttpClient;

var result = await httpClient.GetAsUnionAsync<WeatherForecast>("api/weather/istanbul");

var output = result switch
{
    Result<WeatherForecast>.Ok(var forecast) => $"Temperature: {forecast.Temperature}",
    Result<WeatherForecast>.Error(UnionError.NotFound(var resource)) => $"Not found: {resource}",
    Result<WeatherForecast>.Error(UnionError.Unauthorized) => "Please log in",
    Result<WeatherForecast>.Error(UnionError.Validation(var fields)) => $"Invalid: {string.Join(", ", fields.Values)}",
    Result<WeatherForecast>.Error(UnionError.SystemFailure(var ex)) => $"Server error: {ex.Message}",
    _ => "Unknown error"
};

// POST with automatic response mapping
var created = await httpClient.PostAsUnionAsync<Order>("api/orders", jsonContent);

// PUT
var updated = await httpClient.PutAsUnionAsync<Order>("api/orders/1", jsonContent);

// DELETE
var deleted = await httpClient.DeleteAsUnionAsync<string?>("api/orders/1");
```

### HTTP Status Code Mapping

| HTTP Status Code | Mapped To |
|---|---|
| 200 OK / 201 Created / 202 Accepted | `Result<T>.Ok` |
| 204 No Content | `Result<T>.Ok(default)` |
| 400 Bad Request | `UnionError.Validation` |
| 401 Unauthorized / 403 Forbidden | `UnionError.Unauthorized` |
| 404 Not Found | `UnionError.NotFound` |
| 409 Conflict | `UnionError.Conflict` |
| 5xx / Other | `UnionError.SystemFailure` |

---

## Error Taxonomy

UnionRailway provides a universal, closed set of error types:

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

Because the hierarchy is sealed (private constructor), the compiler can assist with exhaustiveness checking in `switch` expressions, ensuring every error case is handled.

---

## Target Frameworks

| Package | Targets |
|---|---|
| `UnionRailway` | .NET Standard 2.1, .NET 8, .NET 9 |
| `UnionRailway.EntityFrameworkCore` | .NET 8, .NET 9 |
| `UnionRailway.HttpClient` | .NET Standard 2.1, .NET 8, .NET 9 |

---

## License

This project is licensed under the [MIT License](LICENSE).
