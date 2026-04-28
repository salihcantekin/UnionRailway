# Contributing to UnionRailway

Thank you for your interest in contributing to UnionRailway! This document provides guidelines and instructions for contributing.

## Code of Conduct

Be respectful, professional, and inclusive. We're all here to build great software together.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/unionrailway.git
   cd unionrailway
   ```
3. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- .NET 9.0 SDK (for multi-targeting)
- Visual Studio 2022 (17.8+) or Visual Studio Code with C# extension
- Git

### Build the Solution

```bash
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run Benchmarks

```bash
cd tests/UnionRailway.Benchmarks
dotnet run -c Release
```

## Project Structure

```
UnionRailway/
├── src/
│   ├── UnionRailway/                    # Core library
│   ├── UnionRailway.AspNetCore/         # ASP.NET Core integration
│   ├── UnionRailway.AspNetCore.OpenApi/ # OpenAPI metadata
│   ├── UnionRailway.HttpClient/         # HttpClient integration
│   └── UnionRailway.EntityFrameworkCore/# EF Core integration
├── tests/
│   ├── UnionRailway.Tests/              # Unit tests
│   ├── UnionRailway.Benchmarks/         # Performance benchmarks
│   └── UnionRailway.Playground/         # Manual testing
├── docs/                                 # Documentation
└── assets/                               # Icons, images
```

## Coding Guidelines

### General Principles

1. **Follow existing conventions** in the codebase
2. **Keep it simple** - prefer clarity over cleverness
3. **Performance matters** - but measure before optimizing
4. **Consistency** - maintain the existing code style

### C# Conventions

- Use **file-scoped namespaces** (`namespace UnionRailway;`)
- Enable **nullable reference types** (`<Nullable>enable</Nullable>`)
- Use **implicit usings** where appropriate
- Prefer **`ValueTask<T>`** for async operations when appropriate
- Use **`[MethodImpl(MethodImplOptions.AggressiveInlining)]`** for hot-path methods

### Code Style

```csharp
// ✅ Good
public static Rail<T> Ok<T>(T value) => value;

public static async ValueTask<Rail<T>> MapAsync<T, TOut>(
    this Rail<T> result,
    Func<T, ValueTask<TOut>> mapper)
{
    ArgumentNullException.ThrowIfNull(mapper);
    // Implementation...
}

// ❌ Avoid
public static Rail<T> Ok<T>(T value) 
{ 
    return value; 
}
```

### Documentation

- **Public APIs** must have XML documentation comments
- **Examples** in documentation should compile and run
- **Complex logic** should have inline comments explaining "why", not "what"

```csharp
/// <summary>
/// Creates a successful rail carrying <paramref name="value"/>.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <param name="value">The success value to wrap.</param>
/// <returns>A <see cref="Rail{T}"/> containing the success value.</returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static Rail<T> Ok<T>(T value) => value;
```

## Testing Guidelines

### Writing Tests

1. **Use xUnit** (the existing test framework)
2. **Follow AAA pattern**: Arrange, Act, Assert
3. **One assertion per test** (when possible)
4. **Descriptive test names**: `MethodName_Scenario_ExpectedBehavior`

```csharp
[Fact]
public void Ok_WithValue_ErrorIsNull()
{
    // Arrange
    var value = 42;

    // Act
    Rail<int> result = Union.Ok(value);

    // Assert
    Assert.Null(result.Error);
    Assert.True(result.TryGetValue(out var actual));
    Assert.Equal(value, actual);
}
```

### Test Coverage

- **All public APIs** must have tests
- **Error paths** must be tested
- **Edge cases** (null, empty, default) must be tested
- **Async operations** must test cancellation

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests for specific framework
dotnet test --framework net8.0

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Pull Request Process

1. **Update documentation** if you're changing public APIs
2. **Add tests** for new functionality
3. **Run benchmarks** if you're changing core performance paths
4. **Update CHANGELOG.md** (if exists) with your changes
5. **Ensure CI passes** (all tests, build, etc.)

### PR Title Format

```
<type>: <description>

Examples:
feat: Add support for custom error types
fix: Resolve race condition in MapAsync
docs: Update comparison with LanguageExt
perf: Optimize Map operation for value types
test: Add tests for UnionWrapper edge cases
```

### PR Description Template

```markdown
## Description
Brief description of what this PR does.

## Changes
- List of changes made
- Another change

## Testing
How has this been tested?

## Breaking Changes
Are there any breaking changes? If so, what and why?

## Checklist
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] Benchmarks run (if performance-related)
- [ ] CI passes
```

## Adding New Features

### Before Starting

1. **Open an issue** to discuss the feature
2. **Get feedback** from maintainers
3. **Design the API** - consider backward compatibility

### Feature Checklist

- [ ] Implementation in core library
- [ ] Unit tests with >80% coverage
- [ ] XML documentation on public APIs
- [ ] README update (if user-facing)
- [ ] Benchmark (if performance-sensitive)
- [ ] Integration test (if ecosystem adapter)

## Performance Considerations

### When to Benchmark

- Changing core `Rail<T>` or `UnionError` operations
- Adding new operators (`Map`, `Bind`, etc.)
- Modifying hot paths (creation, pattern matching)

### Benchmark Template

```csharp
[Benchmark(Description = "Your operation")]
[MemoryDiagnoser]
public Rail<int> YourOperation()
{
    // Your code
}
```

### Performance Goals

- **Zero allocations** on success path for value types
- **Minimal allocations** on error path
- **Inline hot paths** with `[MethodImpl(AggressiveInlining)]`

## Documentation

### README Updates

- Keep examples **simple and runnable**
- Update badges if adding new features
- Add new packages to the "Packages" section

### Adding New Docs

Place new documentation in `docs/`:

- `docs/COMPARISON.md` - Library comparisons
- `docs/ADVANCED.md` - Advanced scenarios
- `docs/MIGRATION.md` - Migration guides

## Release Process

(For maintainers)

1. Update version in all `.csproj` files
2. Update `CHANGELOG.md`
3. Create a git tag: `git tag v1.2.3`
4. Push tag: `git push origin v1.2.3`
5. GitHub Actions will build and publish to NuGet

## Questions?

- **Open an issue** for questions about contributing
- **Check existing issues** - your question might be answered
- **Join discussions** on GitHub Discussions (if enabled)

## Recognition

All contributors will be recognized in:
- `CONTRIBUTORS.md` (if created)
- Release notes
- Project README

Thank you for contributing to UnionRailway! 🚂
