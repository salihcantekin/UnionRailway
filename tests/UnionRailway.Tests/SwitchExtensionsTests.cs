using UnionRailway;

namespace UnionRailway.Tests;

public sealed class SwitchExtensionsTests
{
    // ── Switch (sync) ───────────────────────────────────────────────────

    [Fact]
    public void Switch_WhenSuccess_CallsOnOk()
    {
        Rail<int> rail = Union.Ok(42);
        int? captured = null;

        rail.Switch(
            onOk: v => captured = v,
            onError: _ => throw new InvalidOperationException("Should not be called"));

        Assert.Equal(42, captured);
    }

    [Fact]
    public void Switch_WhenError_CallsOnError()
    {
        Rail<int> rail = Union.Fail<int>(new UnionError.NotFound("Item"));
        UnionError? captured = null;

        rail.Switch(
            onOk: _ => throw new InvalidOperationException("Should not be called"),
            onError: err => captured = err);

        Assert.NotNull(captured);
        Assert.IsType<UnionError.NotFound>(captured.Value.Value);
    }

    [Fact]
    public void Switch_NullOnOk_ThrowsArgumentNullException()
    {
        Rail<int> rail = Union.Ok(42);

        Assert.Throws<ArgumentNullException>(() =>
            rail.Switch(null!, _ => { }));
    }

    [Fact]
    public void Switch_NullOnError_ThrowsArgumentNullException()
    {
        Rail<int> rail = Union.Ok(42);

        Assert.Throws<ArgumentNullException>(() =>
            rail.Switch(_ => { }, null!));
    }

    // ── SwitchAsync (Task<Rail<T>>) ─────────────────────────────────────

    [Fact]
    public async Task SwitchAsync_Task_WhenSuccess_CallsOnOk()
    {
        Task<Rail<string>> railTask = Task.FromResult(Union.Ok("hello"));
        string? captured = null;

        await railTask.SwitchAsync(
            onOk: v => captured = v,
            onError: _ => throw new InvalidOperationException("Should not be called"));

        Assert.Equal("hello", captured);
    }

    [Fact]
    public async Task SwitchAsync_Task_WhenError_CallsOnError()
    {
        Task<Rail<string>> railTask = Task.FromResult(
            Union.Fail<string>(new UnionError.Conflict("dupe")));
        UnionError? captured = null;

        await railTask.SwitchAsync(
            onOk: _ => throw new InvalidOperationException("Should not be called"),
            onError: err => captured = err);

        Assert.NotNull(captured);
        Assert.IsType<UnionError.Conflict>(captured.Value.Value);
    }

    // ── SwitchAsync (async delegates) ───────────────────────────────────

    [Fact]
    public async Task SwitchAsync_AsyncDelegates_WhenSuccess_CallsOnOk()
    {
        Task<Rail<int>> railTask = Task.FromResult(Union.Ok(10));
        int? captured = null;

        await railTask.SwitchAsync(
            onOk: v => { captured = v; return Task.CompletedTask; },
            onError: _ => throw new InvalidOperationException("Should not be called"));

        Assert.Equal(10, captured);
    }

    [Fact]
    public async Task SwitchAsync_AsyncDelegates_WhenError_CallsOnError()
    {
        Task<Rail<int>> railTask = Task.FromResult(
            Union.Fail<int>(new UnionError.Forbidden("denied")));
        UnionError? captured = null;

        await railTask.SwitchAsync(
            onOk: _ => throw new InvalidOperationException("Should not be called"),
            onError: err => { captured = err; return Task.CompletedTask; });

        Assert.NotNull(captured);
        Assert.IsType<UnionError.Forbidden>(captured.Value.Value);
    }
}
