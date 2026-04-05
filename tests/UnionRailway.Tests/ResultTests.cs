using UnionRailway;

namespace UnionRailway.Tests;

/// <summary>
/// Tests for the zero-allocation Union helpers (<see cref="Union"/>) and
/// the <see cref="UnionExtensions"/> operating on <c>(T Value, UnionError? Error)</c>.
/// </summary>
public sealed class UnionTests
{
    // ── Union.Ok / Union.Fail ──────────────────────────────────────────

    [Fact]
    public void Ok_ErrorIsNull()
    {
        var result = Union.Ok(42);

        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Fail_ErrorIsSet()
    {
        var result = Union.Fail<int>(new UnionError.Unauthorized());

        Assert.NotNull(result.Error);
        Assert.IsType<UnionError.Unauthorized>(result.Error);
    }

    // ── Tuple literals ────────────────────────────────────────────────

    [Fact]
    public void TupleLiteral_SuccessPath_ErrorIsNull()
    {
        (string Value, UnionError? Error) result = ("hello", null);

        Assert.Null(result.Error);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void TupleLiteral_ErrorPath_ErrorIsSet()
    {
        (string Value, UnionError? Error) result = (default!, new UnionError.NotFound("file"));

        Assert.NotNull(result.Error);
        Assert.IsType<UnionError.NotFound>(result.Error);
    }

    // ── IsSuccess ─────────────────────────────────────────────────────

    [Fact]
    public void IsSuccess_WhenOk_ReturnsTrueAndSetsData()
    {
        var result = Union.Ok("world");

        Assert.True(result.IsSuccess(out var data, out var error));
        Assert.Equal("world", data);
        Assert.Null(error);
    }

    [Fact]
    public void IsSuccess_WhenError_ReturnsFalseAndSetsError()
    {
        var result = Union.Fail<string>(new UnionError.NotFound("Thing"));

        Assert.False(result.IsSuccess(out var data, out var error));
        Assert.NotNull(error);
        Assert.IsType<UnionError.NotFound>(error);
    }

    // ── Unwrap ────────────────────────────────────────────────────────

    [Fact]
    public void Unwrap_WhenOk_ReturnsValue()
    {
        var result = Union.Ok(99);

        Assert.Equal(99, result.Unwrap());
    }

    [Fact]
    public void Unwrap_WhenError_ThrowsUnwrapException()
    {
        var result = Union.Fail<int>(new UnionError.Unauthorized());

        var ex = Assert.Throws<UnwrapException>(() => result.Unwrap());
        Assert.IsType<UnionError.Unauthorized>(ex.Error);
    }

    // ── UnwrapOrDefault ───────────────────────────────────────────────

    [Fact]
    public void UnwrapOrDefault_WhenOk_ReturnsValue()
    {
        var result = Union.Ok(5);

        Assert.Equal(5, result.UnwrapOrDefault(-1));
    }

    [Fact]
    public void UnwrapOrDefault_WhenError_ReturnsDefault()
    {
        var result = Union.Fail<int>(new UnionError.Conflict("dupe"));

        Assert.Equal(-1, result.UnwrapOrDefault(-1));
    }

    // ── Match ─────────────────────────────────────────────────────────

    [Fact]
    public void Match_WhenOk_InvokesOnOk()
    {
        var result = Union.Ok("hello");

        var output = result.Match(
            onOk:    v   => $"Got: {v}",
            onError: err => $"Error: {err.GetType().Name}");

        Assert.Equal("Got: hello", output);
    }

    [Fact]
    public void Match_WhenError_InvokesOnError()
    {
        var result = Union.Fail<string>(new UnionError.Forbidden("no role"));

        var output = result.Match(
            onOk:    _ => "ok",
            onError: err => err switch
            {
                UnionError.Forbidden f => $"forbidden: {f.Reason}",
                _                     => "other"
            });

        Assert.Equal("forbidden: no role", output);
    }

    [Fact]
    public void Match_NullOnOk_ThrowsArgumentNullException()
    {
        var result = Union.Ok(1);
        Assert.Throws<ArgumentNullException>(() =>
            result.Match(onOk: (Func<int, string>)null!, onError: _ => ""));
    }

    [Fact]
    public void Match_NullOnError_ThrowsArgumentNullException()
    {
        var result = Union.Ok(1);
        Assert.Throws<ArgumentNullException>(() =>
            result.Match(onOk: i => i.ToString(), onError: (Func<UnionError, string>)null!));
    }

    // ── Union.Combine ─────────────────────────────────────────────────

    [Fact]
    public void Combine_TwoOks_ReturnsTuplePair()
    {
        var r1 = Union.Ok("Alice");
        var r2 = Union.Ok(30);

        var combined = Union.Combine(r1, r2);

        Assert.Null(combined.Error);
        var (name, age) = combined.Unwrap();
        Assert.Equal("Alice", name);
        Assert.Equal(30, age);
    }

    [Fact]
    public void Combine_FirstIsError_ReturnsFirstError()
    {
        var r1 = Union.Fail<string>(UnionError.CreateValidation([("Name", ["Required"])]));
        var r2 = Union.Ok(30);

        var combined = Union.Combine(r1, r2);

        Assert.NotNull(combined.Error);
        Assert.IsType<UnionError.Validation>(combined.Error);
    }

    [Fact]
    public void Combine_SecondIsError_ReturnsSecondError()
    {
        var r1 = Union.Ok("Alice");
        var r2 = Union.Fail<int>(new UnionError.Conflict("dupe"));

        var combined = Union.Combine(r1, r2);

        Assert.NotNull(combined.Error);
        Assert.IsType<UnionError.Conflict>(combined.Error);
    }
}
