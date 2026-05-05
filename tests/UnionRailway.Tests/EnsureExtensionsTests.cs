using UnionRailway;

namespace UnionRailway.Tests;

public sealed class EnsureExtensionsTests
{
    // ── Ensure (sync) ───────────────────────────────────────────────────

    [Fact]
    public void Ensure_WhenPredicateTrue_ReturnsSameSuccess()
    {
        Rail<int> rail = Union.Ok(42);

        Rail<int> result = rail.Ensure(
            v => v > 0,
            _ => new UnionError.Validation(new Dictionary<string, string[]>
            {
                ["Value"] = ["Must be positive"]
            }.AsReadOnly()));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Unwrap());
    }

    [Fact]
    public void Ensure_WhenPredicateFalse_ReturnsError()
    {
        Rail<int> rail = Union.Ok(-1);

        Rail<int> result = rail.Ensure(
            v => v > 0,
            _ => new UnionError.Validation(new Dictionary<string, string[]>
            {
                ["Value"] = ["Must be positive"]
            }.AsReadOnly()));

        Assert.True(result.IsError);
        Assert.IsType<UnionError.Validation>(result.Error.GetValueOrDefault().Value);
    }

    [Fact]
    public void Ensure_WhenAlreadyError_PassesThrough()
    {
        Rail<int> rail = Union.Fail<int>(new UnionError.NotFound("Item"));

        Rail<int> result = rail.Ensure(
            v => v > 0,
            _ => new UnionError.Conflict("should not reach"));

        Assert.True(result.IsError);
        Assert.IsType<UnionError.NotFound>(result.Error.GetValueOrDefault().Value);
    }

    [Fact]
    public void Ensure_WhenNullCheck_ConvertsNullSuccessToError()
    {
        Rail<string?> rail = Union.Ok<string?>(null);

        Rail<string?> result = rail.Ensure(
            v => v is not null,
            _ => new UnionError.NotFound("Resource"));

        Assert.True(result.IsError);
        Assert.IsType<UnionError.NotFound>(result.Error.GetValueOrDefault().Value);
    }

    // ── EnsureAsync (sync predicate on Task<Rail<T>>) ───────────────────

    [Fact]
    public async Task EnsureAsync_Task_WhenPredicateTrue_ReturnsSameSuccess()
    {
        Task<Rail<int>> railTask = Task.FromResult(Union.Ok(10));

        Rail<int> result = await railTask.EnsureAsync(
            v => v > 0,
            _ => new UnionError.Conflict("negative"));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Unwrap());
    }

    [Fact]
    public async Task EnsureAsync_Task_WhenPredicateFalse_ReturnsError()
    {
        Task<Rail<int>> railTask = Task.FromResult(Union.Ok(-5));

        Rail<int> result = await railTask.EnsureAsync(
            v => v > 0,
            _ => new UnionError.Conflict("negative"));

        Assert.True(result.IsError);
        Assert.IsType<UnionError.Conflict>(result.Error.GetValueOrDefault().Value);
    }

    [Fact]
    public async Task EnsureAsync_Task_WhenAlreadyError_PassesThrough()
    {
        Task<Rail<int>> railTask = Task.FromResult(
            Union.Fail<int>(new UnionError.Forbidden("denied")));

        Rail<int> result = await railTask.EnsureAsync(
            v => v > 0,
            _ => new UnionError.Conflict("should not reach"));

        Assert.True(result.IsError);
        Assert.IsType<UnionError.Forbidden>(result.Error.GetValueOrDefault().Value);
    }

    // ── EnsureAsync (async predicate on Task<Rail<T>>) ──────────────────

    [Fact]
    public async Task EnsureAsync_AsyncPredicate_WhenTrue_ReturnsSameSuccess()
    {
        Task<Rail<string>> railTask = Task.FromResult(Union.Ok("hello"));

        Rail<string> result = await railTask.EnsureAsync(
            v => new ValueTask<bool>(v.Length > 0),
            _ => new UnionError.Validation(new Dictionary<string, string[]>
            {
                ["Value"] = ["Cannot be empty"]
            }.AsReadOnly()));

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Unwrap());
    }

    [Fact]
    public async Task EnsureAsync_AsyncPredicate_WhenFalse_ReturnsError()
    {
        Task<Rail<string>> railTask = Task.FromResult(Union.Ok(""));

        Rail<string> result = await railTask.EnsureAsync(
            v => new ValueTask<bool>(v.Length > 0),
            _ => new UnionError.Validation(new Dictionary<string, string[]>
            {
                ["Value"] = ["Cannot be empty"]
            }.AsReadOnly()));

        Assert.True(result.IsError);
        Assert.IsType<UnionError.Validation>(result.Error.GetValueOrDefault().Value);
    }

    // ── Chaining scenario ───────────────────────────────────────────────

    [Fact]
    public async Task Ensure_ChainedWithBind_ShortCircuitsOnFailure()
    {
        var bindCalled = false;

        Rail<string> result = await Task.FromResult(Union.Ok<string?>(null))
            .EnsureAsync(
                v => v is not null,
                _ => new UnionError.NotFound("Transaction"))
            .BindAsync(v =>
            {
                bindCalled = true;
                return Task.FromResult(Union.Ok(v!.ToUpperInvariant()));
            });

        Assert.True(result.IsError);
        Assert.False(bindCalled);
        Assert.IsType<UnionError.NotFound>(result.Error.GetValueOrDefault().Value);
    }
}
