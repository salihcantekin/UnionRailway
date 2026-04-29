using UnionRailway;

namespace UnionRailway.Tests;

public sealed class RecoverExtensionsTests
{
    // ── Recover (sync) ──────────────────────────────────────────────────

    [Fact]
    public void Recover_WhenMatchingError_ReturnsFallbackValue()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.NotFound("User"));

        Rail<string> recovered = result.Recover<string, UnionError.NotFound>(
            nf => $"default-{nf.Resource}");

        Assert.True(recovered.IsSuccess);
        Assert.Equal("default-User", recovered.Unwrap());
    }

    [Fact]
    public void Recover_WhenDifferentError_PassesThrough()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.Conflict("dupe"));

        Rail<string> recovered = result.Recover<string, UnionError.NotFound>(
            _ => "fallback");

        Assert.True(recovered.IsError);
        Assert.IsType<UnionError.Conflict>(recovered.Error.GetValueOrDefault().Value);
    }

    [Fact]
    public void Recover_WhenSuccess_ReturnsOriginalValue()
    {
        Rail<string> result = Union.Ok("hello");

        Rail<string> recovered = result.Recover<string, UnionError.NotFound>(
            _ => "fallback");

        Assert.True(recovered.IsSuccess);
        Assert.Equal("hello", recovered.Unwrap());
    }

    [Fact]
    public void Recover_NullRecovery_ThrowsArgumentNullException()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.NotFound("X"));

        Assert.Throws<ArgumentNullException>(() =>
            result.Recover<string, UnionError.NotFound>(null!));
    }

    // ── RecoverAsync (sync on Rail) ─────────────────────────────────────

    [Fact]
    public async Task RecoverAsync_WhenMatchingError_ReturnsFallbackValue()
    {
        Rail<int> result = Union.Fail<int>(new UnionError.NotFound("Item"));

        Rail<int> recovered = await result.RecoverAsync<int, UnionError.NotFound>(
            _ => new ValueTask<int>(42));

        Assert.True(recovered.IsSuccess);
        Assert.Equal(42, recovered.Unwrap());
    }

    [Fact]
    public async Task RecoverAsync_WhenDifferentError_PassesThrough()
    {
        Rail<int> result = Union.Fail<int>(new UnionError.Unauthorized());

        Rail<int> recovered = await result.RecoverAsync<int, UnionError.NotFound>(
            _ => new ValueTask<int>(42));

        Assert.True(recovered.IsError);
    }

    // ── RecoverAsync (Task<Rail<T>>) ────────────────────────────────────

    [Fact]
    public async Task RecoverAsync_TaskRail_WhenMatchingError_ReturnsFallbackValue()
    {
        Task<Rail<string>> resultTask = Task.FromResult(
            Union.Fail<string>(new UnionError.NotFound("Order")));

        Rail<string> recovered = await resultTask
            .RecoverAsync<string, UnionError.NotFound>(nf => $"recovered-{nf.Resource}");

        Assert.True(recovered.IsSuccess);
        Assert.Equal("recovered-Order", recovered.Unwrap());
    }

    [Fact]
    public async Task RecoverAsync_TaskRail_WhenSuccess_ReturnsOriginalValue()
    {
        Task<Rail<string>> resultTask = Task.FromResult(Union.Ok("original"));

        Rail<string> recovered = await resultTask
            .RecoverAsync<string, UnionError.NotFound>(_ => "fallback");

        Assert.Equal("original", recovered.Unwrap());
    }

    [Fact]
    public async Task RecoverAsync_TaskRail_WithAsyncRecovery_ReturnsFallback()
    {
        Task<Rail<string>> resultTask = Task.FromResult(
            Union.Fail<string>(new UnionError.NotFound("X")));

        Rail<string> recovered = await resultTask
            .RecoverAsync<string, UnionError.NotFound>(
                _ => Task.FromResult("async-fallback"));

        Assert.Equal("async-fallback", recovered.Unwrap());
    }

    // ── RecoverAsync (ValueTask<Rail<T>>) ───────────────────────────────

    [Fact]
    public async Task RecoverAsync_ValueTaskRail_WhenMatchingError_ReturnsFallback()
    {
        ValueTask<Rail<int>> resultTask = new(
            Union.Fail<int>(new UnionError.NotFound("Item")));

        Rail<int> recovered = await resultTask
            .RecoverAsync<int, UnionError.NotFound>(_ => 99);

        Assert.Equal(99, recovered.Unwrap());
    }

    [Fact]
    public async Task RecoverAsync_ValueTaskRail_WithAsyncRecovery_ReturnsFallback()
    {
        ValueTask<Rail<int>> resultTask = new(
            Union.Fail<int>(new UnionError.NotFound("Item")));

        Rail<int> recovered = await resultTask
            .RecoverAsync<int, UnionError.NotFound>(
                _ => new ValueTask<int>(77));

        Assert.Equal(77, recovered.Unwrap());
    }

    // ── Recover with Custom error ───────────────────────────────────────

    [Fact]
    public void Recover_WithCustomError_MatchesCorrectly()
    {
        Rail<string> result = Union.Fail<string>(
            new UnionError.Custom("RATE_LIMIT", "Too many requests", StatusCode: 429));

        Rail<string> recovered = result.Recover<string, UnionError.Custom>(
            c => $"recovered from {c.Code}");

        Assert.Equal("recovered from RATE_LIMIT", recovered.Unwrap());
    }
}
