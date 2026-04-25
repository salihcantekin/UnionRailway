using UnionRailway;
using Xunit;

namespace UnionRailway.Tests;

public sealed class UnionWrapperTests
{
    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ActionReturnsValue_ReturnsOk()
    {
        (string? value, UnionError? error) = await UnionWrapper.RunAsync(() => Task.FromResult("hello"));

        Assert.Null(error);
        Assert.Equal("hello", value);
    }

    // ── Null → NotFound ───────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ActionReturnsNull_ReturnsNotFound()
    {
        (string _, UnionError? error) = await UnionWrapper.RunNullableAsync(() => Task.FromResult<string?>(null));

        UnionError.NotFound notFound = Assert.IsType<UnionError.NotFound>(error.GetValueOrDefault().Value);
        Assert.Equal("Result", notFound.Resource);
    }

    // ── Exception mapping ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_ThrowsUnauthorizedAccessException_ReturnsUnauthorized()
    {
        (string _, UnionError? error) = await UnionWrapper.RunAsync<string>(
            () => throw new UnauthorizedAccessException());

        Assert.IsType<UnionError.Unauthorized>(error.GetValueOrDefault().Value);
    }

    [Fact]
    public async Task RunAsync_ThrowsKeyNotFoundException_ReturnsNotFound()
    {
        const string msg = "item-99";
        (string _, UnionError? error) = await UnionWrapper.RunAsync<string>(
            () => throw new KeyNotFoundException(msg));

        UnionError.NotFound notFound = Assert.IsType<UnionError.NotFound>(error.GetValueOrDefault().Value);
        Assert.Equal(msg, notFound.Resource);
    }

    [Fact]
    public async Task RunAsync_ThrowsGenericException_ReturnsSystemFailure()
    {
        var inner = new InvalidOperationException("boom");
        (string _, UnionError? error) = await UnionWrapper.RunAsync<string>(() => throw inner);

        UnionError.SystemFailure failure = Assert.IsType<UnionError.SystemFailure>(error.GetValueOrDefault().Value);
        Assert.Same(inner, failure.Ex);
    }

    // ── OperationCanceledException is re-thrown, not swallowed ───────────────

    [Fact]
    public async Task RunAsync_ThrowsOperationCanceledException_Rethrows()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            UnionWrapper.RunAsync<string>(() => throw new OperationCanceledException()).AsTask());
    }
}
