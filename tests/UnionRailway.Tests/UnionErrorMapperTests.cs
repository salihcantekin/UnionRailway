using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using UnionRailway;
using UnionRailway.AspNetCore;

namespace UnionRailway.Tests;

public sealed class UnionErrorMapperTests
{
    private sealed class CustomNotFoundMapper : IUnionErrorMapper
    {
        public IResult? TryMap(UnionError error) => error.Value switch
        {
            UnionError.NotFound nf => Results.Problem(
                detail: $"Custom: '{nf.Resource}' missing",
                statusCode: StatusCodes.Status404NotFound,
                title: "Custom Not Found"),
            _ => null
        };
    }

    private sealed class AlwaysNullMapper : IUnionErrorMapper
    {
        public IResult? TryMap(UnionError error) => null;
    }

    // ── TryMap intercepts matching error ─────────────────────────────────

    [Fact]
    public void ToHttpResult_WithMapper_WhenMapperHandlesError_ReturnsCustomResult()
    {
        UnionError error = new UnionError.NotFound("User");
        var mapper = new CustomNotFoundMapper();

        IResult httpResult = error.ToHttpResult(errorMapper: mapper);

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, problemResult.ProblemDetails.Status);
        Assert.Contains("Custom:", problemResult.ProblemDetails.Detail);
        Assert.Equal("Custom Not Found", problemResult.ProblemDetails.Title);
    }

    // ── TryMap returns null → default mapping ───────────────────────────

    [Fact]
    public void ToHttpResult_WithMapper_WhenMapperReturnsNull_FallsBackToDefault()
    {
        UnionError error = new UnionError.Conflict("dupe");
        var mapper = new CustomNotFoundMapper();

        IResult httpResult = error.ToHttpResult(errorMapper: mapper);

        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status409Conflict, statusResult.StatusCode);
    }

    // ── No mapper → default mapping (backward compat) ───────────────────

    [Fact]
    public void ToHttpResult_WithoutMapper_UsesDefaultMapping()
    {
        UnionError error = new UnionError.NotFound("Item");

        IResult httpResult = error.ToHttpResult();

        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    // ── AlwaysNull mapper → always default ──────────────────────────────

    [Fact]
    public void ToHttpResult_WithAlwaysNullMapper_UsesDefaultForAllErrors()
    {
        var mapper = new AlwaysNullMapper();

        UnionError notFound = new UnionError.NotFound("X");
        UnionError conflict = new UnionError.Conflict("Y");
        UnionError unauthorized = new UnionError.Unauthorized();

        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeHttpResult>(notFound.ToHttpResult(errorMapper: mapper)).StatusCode);
        Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeHttpResult>(conflict.ToHttpResult(errorMapper: mapper)).StatusCode);
        Assert.Equal(401, Assert.IsAssignableFrom<IStatusCodeHttpResult>(unauthorized.ToHttpResult(errorMapper: mapper)).StatusCode);
    }

    // ── Mapper + configureProblem both work together ────────────────────

    [Fact]
    public void ToHttpResult_WithMapper_AndConfigureProblem_MapperTakesPriority()
    {
        UnionError error = new UnionError.NotFound("User");
        var mapper = new CustomNotFoundMapper();
        var configureCalled = false;

        IResult httpResult = error.ToHttpResult(
            configureProblem: _ => configureCalled = true,
            errorMapper: mapper);

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Contains("Custom:", problemResult.ProblemDetails.Detail);
        Assert.False(configureCalled);
    }

    [Fact]
    public void ToHttpResult_WithMapper_WhenMapperReturnsNull_ConfigureProblemStillApplied()
    {
        UnionError error = new UnionError.Conflict("dupe");
        var mapper = new CustomNotFoundMapper();

        IResult httpResult = error.ToHttpResult(
            configureProblem: pd => pd.Extensions["custom"] = true,
            errorMapper: mapper);

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status409Conflict, problemResult.ProblemDetails.Status);
        Assert.Equal(true, problemResult.ProblemDetails.Extensions["custom"]);
    }

    // ── Rail<T>.ToHttpResult with mapper ────────────────────────────────

    [Fact]
    public void RailToHttpResult_WithMapper_PropagatesMapper()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.NotFound("Order"));
        var mapper = new CustomNotFoundMapper();

        IResult httpResult = result.ToHttpResult(errorMapper: mapper);

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Contains("Custom:", problemResult.ProblemDetails.Detail);
    }

    [Fact]
    public void RailToHttpResult_WithMapper_SuccessIgnoresMapper()
    {
        Rail<string> result = Union.Ok("hello");
        var mapper = new CustomNotFoundMapper();

        IResult httpResult = result.ToHttpResult(errorMapper: mapper);
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    // ── Async overloads propagate mapper ────────────────────────────────

    [Fact]
    public async Task ToHttpResultAsync_Task_WithMapper_PropagatesMapper()
    {
        Task<Rail<string>> resultTask = Task.FromResult(
            Union.Fail<string>(new UnionError.NotFound("X")));
        var mapper = new CustomNotFoundMapper();

        IResult httpResult = await resultTask.ToHttpResultAsync(errorMapper: mapper);

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Contains("Custom:", problemResult.ProblemDetails.Detail);
    }

    [Fact]
    public async Task ToHttpResultAsync_ValueTask_WithMapper_PropagatesMapper()
    {
        ValueTask<Rail<string>> resultTask = new(
            Union.Fail<string>(new UnionError.NotFound("X")));
        var mapper = new CustomNotFoundMapper();

        IResult httpResult = await resultTask.ToHttpResultAsync(errorMapper: mapper);

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Contains("Custom:", problemResult.ProblemDetails.Detail);
    }

    // ── Mapper with Custom error ────────────────────────────────────────

    [Fact]
    public void ToHttpResult_MapperCanHandleCustomError()
    {
        var mapper = new RateLimitMapper();
        UnionError error = new UnionError.Custom("RATE_LIMIT", "Slow down", StatusCode: 429);

        IResult httpResult = error.ToHttpResult(errorMapper: mapper);

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(429, problemResult.ProblemDetails.Status);
        Assert.Equal("Rate Limited", problemResult.ProblemDetails.Title);
    }

    private sealed class RateLimitMapper : IUnionErrorMapper
    {
        public IResult? TryMap(UnionError error) => error.Value switch
        {
            UnionError.Custom { Code: "RATE_LIMIT" } c => Results.Problem(
                detail: c.Message,
                statusCode: c.StatusCode,
                title: "Rate Limited"),
            _ => null
        };
    }
}
