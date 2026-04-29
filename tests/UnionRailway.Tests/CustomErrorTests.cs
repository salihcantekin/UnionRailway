using UnionRailway;

namespace UnionRailway.Tests;

public sealed class CustomErrorTests
{
    // ── Construction ────────────────────────────────────────────────────

    [Fact]
    public void Custom_CreatesCorrectType()
    {
        var error = new UnionError.Custom("RATE_LIMIT", "Too many requests");

        Assert.Equal("RATE_LIMIT", error.Code);
        Assert.Equal("Too many requests", error.Message);
        Assert.Equal(422, error.StatusCode);
        Assert.Null(error.Extensions);
    }

    [Fact]
    public void Custom_WithStatusCode_SetsStatusCode()
    {
        var error = new UnionError.Custom("GONE", "Resource expired", StatusCode: 410);

        Assert.Equal(410, error.StatusCode);
    }

    [Fact]
    public void Custom_WithExtensions_CarriesMetadata()
    {
        var extensions = new Dictionary<string, object>
        {
            ["retryAfter"] = 30,
            ["limit"] = 100
        };

        var error = new UnionError.Custom(
            "RATE_LIMIT",
            "Too many requests",
            Extensions: extensions);

        Assert.NotNull(error.Extensions);
        Assert.Equal(30, error.Extensions["retryAfter"]);
        Assert.Equal(100, error.Extensions["limit"]);
    }

    // ── Implicit conversion ─────────────────────────────────────────────

    [Fact]
    public void Custom_ConvertsToUnionError()
    {
        UnionError error = new UnionError.Custom("CODE", "msg");

        Assert.IsType<UnionError.Custom>(error.Value);
    }

    [Fact]
    public void Custom_ConvertsToRail()
    {
        Rail<int> result = new UnionError.Custom("CODE", "msg");

        Assert.True(result.IsError);
        Assert.True(result.TryGetError(out var error));
        Assert.IsType<UnionError.Custom>(error.GetValueOrDefault().Value);
    }

    // ── Pattern matching ────────────────────────────────────────────────

    [Fact]
    public void SwitchExpression_MatchesCustom()
    {
        UnionError error = new UnionError.Custom("RATE_LIMIT", "Too many requests", StatusCode: 429);

        var message = error.Value switch
        {
            UnionError.NotFound => "not found",
            UnionError.Custom c => $"custom: {c.Code} ({c.StatusCode})",
            _ => "other"
        };

        Assert.Equal("custom: RATE_LIMIT (429)", message);
    }

    // ── Record equality ─────────────────────────────────────────────────

    [Fact]
    public void Custom_SameValues_AreEqual()
    {
        var a = new UnionError.Custom("CODE", "msg", 422);
        var b = new UnionError.Custom("CODE", "msg", 422);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Custom_DifferentCode_AreNotEqual()
    {
        var a = new UnionError.Custom("A", "msg");
        var b = new UnionError.Custom("B", "msg");

        Assert.NotEqual(a, b);
    }
}
