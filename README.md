# UnionRailway

[![Build](https://img.shields.io/github/actions/workflow/status/salihcantekin/UnionRailway/ci.yml?branch=main&label=build)](https://github.com/salihcantekin/UnionRailway)
[![NuGet](https://img.shields.io/nuget/v/UnionRailway.svg)](https://www.nuget.org/packages/UnionRailway)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-57%20passing-brightgreen)](tests/UnionRailway.Tests)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2011.0-purple)](https://dotnet.microsoft.com)

> Native-union-ready railway programming for C#.

UnionRailway is a result-flow library for C# applications that want typed, composable failures without relying on exceptions for normal control flow.

It is intentionally shaped around the upcoming C# union model:

- `Rail<T>` is the core result union: success value or `UnionError`
- `UnionError` is a closed semantic error union
- adapters for ASP.NET Core, HttpClient, and Entity Framework Core preserve the same error vocabulary end-to-end

---

## Why UnionRailway instead of other C# railway/result libraries?

Most existing libraries were created to work around the absence of native unions in C#. UnionRailway takes a different path.

### 1. Native-union-first direction

UnionRailway is designed so its core abstractions can move naturally toward native C# unions as the language matures.

Today it uses custom union-compatible shapes:

- `Rail<T>`
- `UnionError`
- preview language support for consuming those custom unions cleanly

On `.NET 11`, the library now uses native `union` declarations for those same shapes while keeping the same public API names.

This makes the library structurally aligned with the direction of the language instead of building an alternative ecosystem that ignores it.

### 2. Semantic errors, not generic string bags

Many result libraries expose generic failures and leave meaning to strings or open-ended metadata.
UnionRailway ships with a shared semantic error model:

- `NotFound`
- `Conflict`
- `Unauthorized`
- `Forbidden`
- `Validation`
- `SystemFailure`

That gives application, infrastructure, and HTTP layers a common language.

### Why `Rail<T>` instead of `Result<T>`?

The library intentionally avoids `Result<T>` as the primary type name.

- `Result` is already overloaded in the .NET ecosystem
- web projects already have `Results` / `IResult` concepts nearby
- many teams already have their own `Result<T>` type

`Rail<T>` is short, specific to the library's purpose, and avoids those naming collisions while still reading naturally in service signatures.

### 3. Ecosystem adapters, not only a core type

UnionRailway is not just a `Result<T>`-like wrapper.
It includes dedicated integrations for:

- `UnionRailway.AspNetCore`
- `UnionRailway.AspNetCore.OpenApi`
- `UnionRailway.HttpClient`
- `UnionRailway.EntityFrameworkCore`

This lets the same typed error flow from database access to outbound HTTP to incoming API responses.

### 4. RFC 7807 support out of the box

ASP.NET Core integration maps `UnionError` directly to `ProblemDetails` / `ValidationProblemDetails`.

```csharp
return result.ToHttpResult();
```

### 5. Better migration story for existing code

Legacy exception-based code can be wrapped immediately:

```csharp
var result = await UnionWrapper.RunAsync(() => service.LoadAsync());
var maybeUser = await UnionWrapper.RunNullableAsync(() => repository.FindAsync(id));
```

---

## Core concepts

## `Rail<T>`

`Rail<T>` represents exactly one of these outcomes:

- a success value of type `T`
- a `UnionError`

```csharp
public async ValueTask<Rail<User>> GetUserAsync(int id)
{
    var user = await db.Users.FirstOrDefaultAsUnionAsync("User", x => x.Id == id);
    return user;
}
```

You can still construct results explicitly:

```csharp
return Union.Ok(user);
return Union.Fail<User>(new UnionError.NotFound("User"));
```

## `UnionError`

`UnionError` is the shared error contract across the whole library.

```csharp
UnionError notFound = new UnionError.NotFound("User");
UnionError validation = UnionError.CreateValidation([
    ("Email", ["Invalid format"]),
    ("Password", ["Required"])
]);
```

Consume it with pattern matching on the wrapped value:

```csharp
var message = error.Value switch
{
    UnionError.NotFound nf      => $"Missing: {nf.Resource}",
    UnionError.Conflict c       => $"Conflict: {c.Reason}",
    UnionError.Unauthorized     => "Authentication required",
    UnionError.Forbidden f      => $"Forbidden: {f.Reason}",
    UnionError.Validation v     => $"Validation: {v.Fields.Count} fields",
    UnionError.SystemFailure sf => sf.Ex.Message,
    _                           => "Unknown error"
};
```

---

## Railway composition

UnionRailway supports both pragmatic early-return code and railway-style chaining.

### Early return

```csharp
var result = await service.GetUserAsync(id);

if (!result.IsSuccess(out var user, out var error))
    return error.GetValueOrDefault().ToHttpResult();

return Results.Ok(user);
```

If a caller only wants to inspect whether a failure exists, `Rail<T>` also exposes a convenience `Error` property:

```csharp
if (result.Error is not null)
    return result.Error.GetValueOrDefault().ToHttpResult();
```

`IsSuccess`, `IsError`, and `Match` remain the preferred APIs when you want to preserve the full union semantics explicitly.

### `Match`

```csharp
return result.Match(
    onOk: user => Results.Ok(user),
    onError: error => error.ToHttpResult());
```

### `Map` and `Bind`

```csharp
var result = Union.Ok(5)
    .Map(x => x * 2)
    .Bind(x => x > 5
        ? Union.Ok($"value={x}")
        : Union.Fail<string>(new UnionError.Conflict("Too small")));
```

### Async `Task<Rail<T>>` and `ValueTask<Rail<T>>` composition

UnionRailway also provides first-class async extensions so callers do not need a separate `TaskRail<T>` wrapper type.

```csharp
var result = await service.GetUserAsync(id)
    .BindAsync(user => service.GetOrdersAsync(user.Id))
    .MapAsync(orders => orders.Count);

var httpResult = await service.GetUserAsync(id).ToHttpResultAsync();
```

## .NET 11 native union direction

When targeting `.NET 11`, the library uses native union declarations for its core types.
Conceptually, the shapes are:

```csharp
public union Rail<T>(T, UnionError);

public union UnionError(
    UnionError.NotFound,
    UnionError.Conflict,
    UnionError.Unauthorized,
    UnionError.Forbidden,
    UnionError.Validation,
    UnionError.SystemFailure);
```

That keeps UnionRailway aligned with the language instead of building a permanently separate abstraction model.

---

## Adapters

### ASP.NET Core

```csharp
app.MapGet("/users/{id:int}", async (int id, UserService service) =>
{
    var result = await service.GetUserAsync(id);
    return result.ToHttpResult();
});
```

This keeps runtime behavior and OpenAPI output aligned: the same library-level error taxonomy that drives `ToHttpResult()` also becomes visible to Swagger consumers.

### ASP.NET Core OpenAPI

Use `UnionRailway.AspNetCore.OpenApi` to advertise the standard `Rail<T>` response set in Minimal API metadata.

Simple default convention:

```csharp
app.MapGet("/users/{id:int}", async (int id, UserService service) =>
        await service.GetUserAsync(id).ToHttpResultAsync())
    .WithRailOpenApi<RouteHandlerBuilder, UserDto>();
```

Created response convention:

```csharp
app.MapPost("/users", async (CreateUserRequest request, UserService service) =>
        await service.CreateAsync(request).ToHttpResultAsync(createdUri: "/users/1"))
    .WithCreatedRailOpenApi<RouteHandlerBuilder, UserDto>();
```

Second version with customization:

```csharp
app.MapGet("/orders/{id:int}", async (int id, OrderService service) =>
        await service.GetOrderAsync(id).ToHttpResultAsync())
    .WithRailOpenApi<RouteHandlerBuilder, OrderDto>(options =>
    {
        options.SuccessStatusCode = StatusCodes.Status202Accepted;
        options.IncludeUnauthorized = false;
        options.IncludeForbidden = false;
        options.IncludeSystemFailure = false;
    });
```

### Entity Framework Core

```csharp
public ValueTask<Rail<User>> GetUserAsync(int id, CancellationToken ct = default) =>
    db.Users.FirstOrDefaultAsUnionAsync("User", x => x.Id == id, ct);
```

### HttpClient

```csharp
var result = await http.GetFromJsonAsUnionAsync<UserDto>("/users/42", ct);
```

---

## Release automation

The repository includes a GitHub Actions workflow at `.github/workflows/ci.yml`.

- pull requests to `main` run the test matrix for `net8.0`, `net9.0`, and `net11.0`
- pushes to `main` run validation only
- tag pushes like `v1.2.3` publish stable NuGet versions
- manual runs require an explicit package version through `workflow_dispatch`

To publish to NuGet.org, configure this repository secret:

- `NUGET_API_KEY`

---

## Packages

### `UnionRailway`
Core result type, error type, helpers, and legacy migration wrappers.

### `UnionRailway.AspNetCore`
`Rail<T>` / `UnionError` to `IResult` conversion with RFC 7807 mapping.

### `UnionRailway.AspNetCore.OpenApi`
OpenAPI metadata conventions for Minimal API endpoints returning `Rail<T>`, with both default and customizable response sets.

Install it when you want Swagger / OpenAPI documents to describe the same success and error responses your `Rail<T>` endpoints actually produce.

### `UnionRailway.HttpClient`
HTTP and problem-details responses to `Rail<T>` conversion.

### `UnionRailway.EntityFrameworkCore`
EF Core queries and save operations to `Rail<T>` conversion.

---

## Current implementation note

The project now multi-targets .NET 8, .NET 9, and .NET 11.
Preview language support remains enabled so the custom union model can be consumed consistently across all targets.
The current .NET 11 preview still requires the temporary union runtime polyfill declared in this repository; that polyfill can be removed once the runtime ships those types.
