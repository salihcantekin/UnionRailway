# UnionRailway - Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **NuGet Package Icon**: Added icon.png to all packages for better visibility on NuGet.org
- **Benchmark Project**: Added comprehensive BenchmarkDotNet suite for performance testing
  - Core operations benchmarks (Create, Map, Bind)
  - Railway chaining benchmarks
  - Async operations benchmarks
  - Real-world scenario benchmarks
- **`BindObject` Extension Methods**: Added type-safe helpers for working with `object` results
  - `BindObject<T>(Func<T, Rail<object>>)` - Classic style for object binding
  - `BindObject<T>(Func<T, bool>, Func<T, object>, Func<T, UnionError>)` - Predicate style (cleaner!)
  - Prevents C# generic type inference issues when using anonymous types
- **`Union.Ok(object)` Overload**: Added explicit object overload to prevent inference issues
  - Safely boxes values for HTTP response serialization
  - Recommended for use with anonymous types in DTOs
- **Integration Test Suite**: Added comprehensive integration tests for demo application
  - 49 integration tests covering all Step01-Step07 endpoints
  - Tests for success paths, error paths, and edge cases
  - Custom `WebApplicationFactory` with TestHost workaround for .NET 8 ProblemDetails serialization
- **Documentation**:
  - Added `docs/COMPARISON.md` with detailed comparison vs LanguageExt, ErrorOr, OneOf, FluentResults
  - Added `CONTRIBUTING.md` with contribution guidelines
  - Added Performance section to README with benchmark results
  - Added **Best Practices & Common Pitfalls** section to README
  - Added detailed XML documentation for `BindObject` and `Union.Ok(object)` with examples
  - Added "Working with Anonymous Types & Object Results" guide in README

### Changed
- **Multi-Framework Support**: Projects now target both .NET 8.0 and .NET 11.0
  - All core libraries (`UnionRailway`, `UnionRailway.AspNetCore`, `UnionRailway.HttpClient`, `UnionRailway.EntityFrameworkCore`, `UnionRailway.AspNetCore.OpenApi`) support both frameworks
  - All test and demo projects also support both frameworks
  - Native union support is ready to be enabled when .NET 11 stable is released with finalized union types
  - Currently uses high-performance struct-based implementation on all frameworks (0.5ns success path, zero allocations)
  - Union types are available in .NET 11 preview builds but not yet enabled in the library until .NET 11 RTM
- **Repository URLs**: Updated all placeholder URLs in `Directory.Build.props`
  - Old: `https://github.com/yourorg/UnionRailway`
  - New: `https://github.com/salihcantekin/unionrailway`
- **README**: Updated .NET version badges and documentation to reflect .NET 8 and .NET 11 multi-targeting
- **Future Native Unions Section**: Clarified that native union support will be automatically enabled when .NET 11 stable is released
- **Step05 Demo Endpoint**: Updated `/bind` endpoint to use new `BindObject` predicate style

### Fixed
- Build errors related to unsupported .NET 11 SDK
- Test execution now passes successfully (180 tests passing: 131 unit + 49 integration)
- **Generic Type Inference Issue**: Fixed `Bind<T, object>` serializing Rail wrapper instead of inner value
  - Root cause: C# generic inference with anonymous types in lambda returns
  - Solution: New `BindObject` methods with explicit `object` constraint
  - Impact: Prevents subtle serialization bugs when returning anonymous types from `Bind`

### Performance
- All new methods use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for zero overhead
- `BindObject` predicate style has same performance as classic if/else in lambda
- Zero breaking changes: existing code continues to work as before

## [Previous Releases]

See Git history for previous changes.
