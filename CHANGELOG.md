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
- **Documentation**:
  - Added `docs/COMPARISON.md` with detailed comparison vs LanguageExt, ErrorOr, OneOf, FluentResults
  - Added `CONTRIBUTING.md` with contribution guidelines
  - Added Performance section to README with benchmark results
- **README Improvements**:
  - Added link to detailed library comparison
  - Added performance benchmarks section
  - Added sample benchmark results

### Changed
- **Target Framework**: Changed from .NET 8/9/11 multi-targeting to .NET 8 only
  - Reason: .NET 9 runtime not available on current system, .NET 11 not released yet
  - Future: Easy to re-enable multi-targeting when newer runtimes are installed
- **Repository URLs**: Updated all placeholder URLs in `Directory.Build.props`
  - Old: `https://github.com/yourorg/UnionRailway`
  - New: `https://github.com/salihcantekin/unionrailway`
- **README**: Updated .NET version badges and documentation to reflect .NET 8 target
- **Future .NET 11 Section**: Clarified that native union support is planned for future .NET 11 release

### Fixed
- Build errors related to unsupported .NET 11 SDK
- Test execution now passes successfully (71 tests passing)

## [Previous Releases]

See Git history for previous changes.
