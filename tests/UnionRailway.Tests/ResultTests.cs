using UnionRailway;

namespace UnionRailway.Tests;

/// <summary>
/// Tests for the rail helpers (<see cref="Union"/>) and
/// the <see cref="UnionExtensions"/> operating on <see cref="Rail{T}"/>.
/// </summary>
public sealed class UnionTests
{
    private static TError AssertError<T, TError>(Rail<T> result)
        where TError : class
    {
        Assert.True(result.TryGetError(out UnionError? error));
        return Assert.IsType<TError>(error.GetValueOrDefault().Value);
    }

    // ── Union.Ok / Union.Fail ──────────────────────────────────────────

    [Fact]
    public void Ok_ErrorIsNull()
    {
        Rail<int> result = Union.Ok(42);

        Assert.False(result.TryGetError(out _));
        Assert.Equal(42, result.Unwrap());
    }

    [Fact]
    public void Fail_ErrorIsSet()
    {
        Rail<int> result = Union.Fail<int>(new UnionError.Unauthorized());

        Assert.NotNull(result.Error);
        AssertError<int, UnionError.Unauthorized>(result);
    }

    [Fact]
    public void Error_WhenOk_ReturnsNull()
    {
        Rail<int> result = Union.Ok(42);

        Assert.Null(result.Error);
    }

    [Fact]
    public void Error_WhenFailed_ReturnsWrappedError()
    {
        Rail<int> result = Union.Fail<int>(new UnionError.Conflict("dupe"));

        Assert.NotNull(result.Error);
        Assert.IsType<UnionError.Conflict>(result.Error.GetValueOrDefault().Value);
    }

    // ── Deconstruction ────────────────────────────────────────────────

    [Fact]
    public void Deconstruct_SuccessPath_ErrorIsNull()
    {
        Rail<string> result = Union.Ok("hello");
        (string? value, UnionError? error) = result;

        Assert.Null(error);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void Deconstruct_ErrorPath_ErrorIsSet()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.NotFound("file"));
        (string _, UnionError? error) = result;

        Assert.NotNull(error);
        Assert.IsType<UnionError.NotFound>(error.GetValueOrDefault().Value);
    }

    // ── IsSuccess ─────────────────────────────────────────────────────

    [Fact]
    public void IsSuccess_WhenOk_ReturnsTrueAndSetsData()
    {
        Rail<string> result = Union.Ok("world");

        Assert.True(result.IsSuccess(out var data, out UnionError? error));
        Assert.Equal("world", data);
        Assert.Null(error);
    }

    [Fact]
    public void IsSuccess_WhenError_ReturnsFalseAndSetsError()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.NotFound("Thing"));

        Assert.False(result.IsSuccess(out var data, out UnionError? error));
        Assert.NotNull(error);
        Assert.IsType<UnionError.NotFound>(error.GetValueOrDefault().Value);
    }

    // ── Unwrap ────────────────────────────────────────────────────────

    [Fact]
    public void Unwrap_WhenOk_ReturnsValue()
    {
        Rail<int> result = Union.Ok(99);

        Assert.Equal(99, result.Unwrap());
    }

    [Fact]
    public void Unwrap_WhenError_ThrowsUnwrapException()
    {
        Rail<int> result = Union.Fail<int>(new UnionError.Unauthorized());

        UnwrapException ex = Assert.Throws<UnwrapException>(() => result.Unwrap());
        Assert.NotNull(ex.Error);
        Assert.IsType<UnionError.Unauthorized>(ex.Error.GetValueOrDefault().Value);
    }

    // ── UnwrapOrDefault ───────────────────────────────────────────────

    [Fact]
    public void UnwrapOrDefault_WhenOk_ReturnsValue()
    {
        Rail<int> result = Union.Ok(5);

        Assert.Equal(5, result.UnwrapOrDefault(-1));
    }

    [Fact]
    public void UnwrapOrDefault_WhenError_ReturnsDefault()
    {
        Rail<int> result = Union.Fail<int>(new UnionError.Conflict("dupe"));

        Assert.Equal(-1, result.UnwrapOrDefault(-1));
    }

    // ── Match ─────────────────────────────────────────────────────────

    [Fact]
    public void Match_WhenOk_InvokesOnOk()
    {
        Rail<string> result = Union.Ok("hello");

        var output = result.Match(
            onOk:    v   => $"Got: {v}",
            onError: err => $"Error: {err.GetType().Name}");

        Assert.Equal("Got: hello", output);
    }

    [Fact]
    public void Match_WhenError_InvokesOnError()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.Forbidden("no role"));

        var output = result.Match(
            onOk:    _ => "ok",
            onError: err => err.Value switch
            {
                UnionError.Forbidden f => $"forbidden: {f.Reason}",
                _                     => "other"
            });

        Assert.Equal("forbidden: no role", output);
    }

    [Fact]
    public void Match_NullOnOk_ThrowsArgumentNullException()
    {
        Rail<int> result = Union.Ok(1);
        Assert.Throws<ArgumentNullException>(() =>
            result.Match(onOk: (Func<int, string>)null!, onError: _ => ""));
    }

    [Fact]
    public void Match_NullOnError_ThrowsArgumentNullException()
    {
        Rail<int> result = Union.Ok(1);
        Assert.Throws<ArgumentNullException>(() =>
            result.Match(onOk: i => i.ToString(), onError: (Func<UnionError, string>)null!));
    }

    // ── Union.Combine ─────────────────────────────────────────────────

    [Fact]
    public void Combine_TwoOks_ReturnsTuplePair()
    {
        Rail<string> r1 = Union.Ok("Alice");
        Rail<int> r2 = Union.Ok(30);

        Rail<(string First, int Second)> combined = Union.Combine(r1, r2);

        (string? name, int age) = combined.Unwrap();
        Assert.Equal("Alice", name);
        Assert.Equal(30, age);
    }

    [Fact]
    public void Combine_FirstIsError_ReturnsFirstError()
    {
        Rail<string> r1 = Union.Fail<string>(UnionError.CreateValidation([("Name", ["Required"])]));
        Rail<int> r2 = Union.Ok(30);

        Rail<(string First, int Second)> combined = Union.Combine(r1, r2);

        AssertError<(string First, int Second), UnionError.Validation>(combined);
    }

    [Fact]
    public void Combine_SecondIsError_ReturnsSecondError()
    {
        Rail<string> r1 = Union.Ok("Alice");
        Rail<int> r2 = Union.Fail<int>(new UnionError.Conflict("dupe"));

        Rail<(string First, int Second)> combined = Union.Combine(r1, r2);

        AssertError<(string First, int Second), UnionError.Conflict>(combined);
    }

    [Fact]
    public void Map_WhenOk_TransformsValue()
    {
        Rail<int> result = Union.Ok(5).Map(x => x * 2);

        Assert.Equal(10, result.Unwrap());
    }

    [Fact]
    public void Bind_WhenError_PropagatesError()
    {
        Rail<int> result = Union.Fail<int>(new UnionError.Conflict("dupe"))
            .Bind(x => Union.Ok(x * 2));

        AssertError<int, UnionError.Conflict>(result);
    }
}
