# UnionRailway vs Other Result Libraries

This document compares UnionRailway with other popular C# result/railway libraries.

## Quick Comparison Table

| Feature | UnionRailway | LanguageExt | ErrorOr | OneOf | FluentResults |
|---------|--------------|-------------|---------|-------|---------------|
| Native Union Support | ✅ (.NET 11+) | ❌ | ❌ | ❌ | ❌ |
| Semantic Error Model | ✅ Closed union | ❌ Open-ended | ✅ Custom errors | ❌ Any type | ❌ String-based |
| ASP.NET Core Integration | ✅ RFC 7807 | ❌ | ❌ | ❌ | ❌ |
| EF Core Integration | ✅ Built-in | ❌ | ❌ | ❌ | ❌ |
| HttpClient Integration | ✅ Built-in | ❌ | ❌ | ❌ | ❌ |
| OpenAPI Support | ✅ Built-in | ❌ | ❌ | ❌ | ❌ |
| Railway Operators | ✅ Map/Bind | ✅ Extensive | ✅ Basic | ❌ | ✅ Basic |
| Async-First | ✅ ValueTask | ✅ Task | ✅ Task | ❌ | ✅ Task |
| Pattern Matching | ✅ Native | ✅ Custom | ✅ Custom | ✅ Native | ❌ |
| Learning Curve | Low | High | Low | Medium | Low |

---

## Detailed Comparisons

### vs LanguageExt

**LanguageExt** is a comprehensive functional programming library inspired by Haskell.

#### LanguageExt Approach:
```csharp
using LanguageExt;
using LanguageExt.Common;

Fin<User> result = await GetUserAsync(id);

return result.Match(
    Succ: user => Results.Ok(user),
    Fail: error => Results.Problem(error.Message)
);
```

#### UnionRailway Approach:
```csharp
using UnionRailway;
using UnionRailway.AspNetCore;

var result = await GetUserAsync(id);
return result.ToHttpResult(); // RFC 7807 automatic
```

**Key Differences:**

| Aspect | LanguageExt | UnionRailway |
|--------|-------------|--------------|
| **Philosophy** | Comprehensive FP library | Focused railway library |
| **API Surface** | 100+ operators | ~15 core methods |
| **Error Model** | `Error` (string-based) | `UnionError` (semantic types) |
| **HTTP Integration** | Manual mapping required | Built-in RFC 7807 |
| **Learning Curve** | Steep (FP concepts) | Gentle (familiar patterns) |
| **Performance** | Heavy allocations | `ValueTask` optimized |

**When to Choose:**
- **LanguageExt**: You want a full FP ecosystem (Option, Either, Try, IO, etc.)
- **UnionRailway**: You want pragmatic result handling with ecosystem integration

---

### vs ErrorOr

**ErrorOr** is a lightweight discriminated union library for error handling.

#### ErrorOr Approach:
```csharp
using ErrorOr;

ErrorOr<User> result = await GetUserAsync(id);

return result.Match(
    value => Results.Ok(value),
    errors => Results.Problem(string.Join(", ", errors.Select(e => e.Description)))
);
```

#### UnionRailway Approach:
```csharp
using UnionRailway;
using UnionRailway.AspNetCore;

var result = await GetUserAsync(id);
return result.ToHttpResult();
```

**Key Differences:**

| Aspect | ErrorOr | UnionRailway |
|--------|---------|--------------|
| **Error Model** | `List<Error>` (custom types) | `UnionError` (closed union) |
| **Multiple Errors** | ✅ Built-in list | ❌ Single error (use Validation) |
| **HTTP Integration** | ❌ Manual | ✅ Automatic RFC 7807 |
| **Ecosystem** | Core only | ASP.NET, EF, HttpClient |
| **Union Alignment** | ❌ No | ✅ .NET 11 native unions |

**When to Choose:**
- **ErrorOr**: You need to collect multiple errors in one result
- **UnionRailway**: You want single-error semantics with ecosystem integration

---

### vs OneOf

**OneOf** is a generic discriminated union library (not result-specific).

#### OneOf Approach:
```csharp
using OneOf;

OneOf<User, NotFound, ServerError> result = await GetUserAsync(id);

return result.Match(
    user => Results.Ok(user),
    notFound => Results.NotFound(),
    serverError => Results.Problem(serverError.Message)
);
```

#### UnionRailway Approach:
```csharp
using UnionRailway;
using UnionRailway.AspNetCore;

Rail<User> result = await GetUserAsync(id);
return result.ToHttpResult();
```

**Key Differences:**

| Aspect | OneOf | UnionRailway |
|--------|-------|--------------|
| **Purpose** | Generic union type | Result-specific railway |
| **Error Model** | Any types you define | Standard `UnionError` taxonomy |
| **Railway Operators** | ❌ None | ✅ Map, Bind, Match |
| **HTTP Integration** | ❌ Manual | ✅ Automatic RFC 7807 |
| **Consistency** | Different signatures everywhere | Consistent `Rail<T>` everywhere |

**When to Choose:**
- **OneOf**: You need generic unions beyond result handling
- **UnionRailway**: You want result-specific railway with built-in error semantics

---

### vs FluentResults

**FluentResults** is a result library with fluent API for building results.

#### FluentResults Approach:
```csharp
using FluentResults;

Result<User> result = await GetUserAsync(id);

if (result.IsFailed)
{
    var error = result.Errors.First();
    return Results.Problem(error.Message);
}

return Results.Ok(result.Value);
```

#### UnionRailway Approach:
```csharp
using UnionRailway;
using UnionRailway.AspNetCore;

var result = await GetUserAsync(id);
return result.ToHttpResult();
```

**Key Differences:**

| Aspect | FluentResults | UnionRailway |
|--------|---------------|--------------|
| **Error Model** | `List<IError>` (open) | `UnionError` (closed) |
| **Type Safety** | ❌ Runtime casting | ✅ Compile-time safety |
| **HTTP Integration** | ❌ Manual | ✅ Automatic RFC 7807 |
| **Pattern Matching** | ❌ No native support | ✅ Native switch expressions |
| **Metadata** | ✅ Rich metadata | ✅ Semantic types as metadata |

**When to Choose:**
- **FluentResults**: You want rich error metadata and logging integration
- **UnionRailway**: You want type-safe errors with pattern matching

---

## Code Examples Side-by-Side

### Scenario: Fetch user by ID with error handling

#### LanguageExt
```csharp
public async Task<IResult> GetUser(int id)
{
    Fin<User> result = await userService.GetAsync(id);

    return result.Match(
        Succ: user => Results.Ok(user),
        Fail: error => error.Code switch
        {
            404 => Results.NotFound(),
            401 => Results.Unauthorized(),
            _ => Results.Problem(error.Message)
        }
    );
}
```

#### ErrorOr
```csharp
public async Task<IResult> GetUser(int id)
{
    ErrorOr<User> result = await userService.GetAsync(id);

    return result.Match(
        value => Results.Ok(value),
        errors => errors[0].Type switch
        {
            ErrorType.NotFound => Results.NotFound(),
            ErrorType.Unauthorized => Results.Unauthorized(),
            _ => Results.Problem(errors[0].Description)
        }
    );
}
```

#### UnionRailway
```csharp
public async Task<IResult> GetUser(int id)
{
    var result = await userService.GetAsync(id);
    return result.ToHttpResult(); // ✅ Automatic RFC 7807 mapping
}
```

---

## Performance Comparison

### Real Benchmark Results

```
BenchmarkDotNet=v0.14.0, OS=Windows, .NET 8.0
```

**UnionRailway Performance:**

| Operation | Mean | Allocated | Notes |
|-----------|------|-----------|-------|
| **Creation** |
| Create success Rail<T> | 0.5 ns | 0 B | ✅ Stack-only, zero allocation |
| Create failure Rail<T> | 2.8 ns | 24 B | Error record allocation |
| Implicit conversion | <1 ns | 0 B | Zero overhead |
| **Pattern Matching** |
| IsSuccess pattern | 0.6 ns | 0 B | Inline check |
| Match pattern | 0.8 ns | 0 B | Zero-cost abstraction |
| TryGetValue pattern | <1 ns | 0 B | Direct struct access |
| **Railway Operations** |
| Map (success) | 1.3 ns | 0 B | AggressiveInlining |
| Map (error) | 3.6 ns | 24 B | Short-circuit |
| Bind (success) | 36 ns | 40 B | Function call overhead |
| Chain 3x Map | 7.2 ns | 0 B | Zero-allocation chain |
| **Async Operations** |
| MapAsync | 48 ns | 0 B | ValueTask overhead only |
| BindAsync | 100 ns | 40 B | Minimal async allocation |
| **Error Handling** |
| NotFound error | 1.8 ns | 24 B | Simple error record |
| Validation error | 103 ns | 464 B | Dictionary creation |
| **Real-World Scenarios** |
| Service call chain (3 ops) | 20 ns | 120 B | Practical overhead |
| Error handling scenario | 4.6 ns | 24 B | Fast failure path |

### Key Performance Wins

✅ **Zero-allocation success path** - struct-based Rail<T> lives on stack  
✅ **Inline operations** - Map/Bind with AggressiveInlining  
✅ **ValueTask async** - 48-100ns overhead vs Task's 200+ns  
✅ **Minimal error cost** - 24B for simple errors, 464B for validation  

### Comparison vs Alternatives

*Note: Comparative benchmarks coming soon. These libraries have different design trade-offs:*

- **LanguageExt**: Rich FP ecosystem but heavier allocations
- **ErrorOr**: Multiple error support but more allocations per result
- **OneOf**: Generic unions but no railway operators
- **FluentResults**: Metadata-rich but runtime casting overhead

*Run benchmarks yourself: `dotnet run -c Release` in `tests/UnionRailway.Benchmarks`*

---

## Migration Guide

### From LanguageExt

```csharp
// Before: LanguageExt
Fin<User> result = await repo.GetAsync(id);
var user = result.Match(
    Succ: u => u,
    Fail: _ => throw new Exception()
);

// After: UnionRailway
Rail<User> result = await repo.GetAsync(id);
var user = result.Unwrap(); // or .Match()
```

### From ErrorOr

```csharp
// Before: ErrorOr
ErrorOr<User> result = await repo.GetAsync(id);
if (result.IsError) return result.Errors[0];

// After: UnionRailway
Rail<User> result = await repo.GetAsync(id);
if (!result.IsSuccess(out var user, out var error))
    return error.GetValueOrDefault();
```

---

## Summary

**Choose UnionRailway when:**
- ✅ You want native union alignment (.NET 11+)
- ✅ You need semantic, type-safe error modeling
- ✅ You want built-in ASP.NET Core, EF Core, HttpClient integration
- ✅ You prefer pragmatic railway over heavy FP
- ✅ You want RFC 7807 compliance out of the box

**Choose alternatives when:**
- ❌ You need comprehensive FP ecosystem → **LanguageExt**
- ❌ You need to collect multiple errors → **ErrorOr**
- ❌ You need generic unions beyond results → **OneOf**
- ❌ You need rich error metadata and logging → **FluentResults**
