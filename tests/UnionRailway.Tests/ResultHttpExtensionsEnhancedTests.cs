using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using UnionRailway;
using UnionRailway.AspNetCore;

namespace UnionRailway.Tests;

public sealed class ResultHttpExtensionsEnhancedTests
{
    // ── Unit → 204 No Content ───────────────────────────────────────────

    [Fact]
    public void ToHttpResult_UnitSuccess_Returns204NoContent()
    {
        Rail<Unit> result = Union.Ok();

        IResult httpResult = result.ToHttpResult();

        Assert.IsType<NoContent>(httpResult);
    }

    [Fact]
    public async Task ToHttpResultAsync_TaskUnitSuccess_Returns204NoContent()
    {
        Task<Rail<Unit>> resultTask = Task.FromResult(Union.Ok());

        IResult httpResult = await resultTask.ToHttpResultAsync();

        Assert.IsType<NoContent>(httpResult);
    }

    [Fact]
    public async Task ToHttpResultAsync_ValueTaskUnitSuccess_Returns204NoContent()
    {
        ValueTask<Rail<Unit>> resultTask = new(Union.Ok());

        IResult httpResult = await resultTask.ToHttpResultAsync();

        Assert.IsType<NoContent>(httpResult);
    }

    // ── Custom error → HTTP ─────────────────────────────────────────────

    [Fact]
    public void ToHttpResult_CustomError_ReturnsCustomStatusCode()
    {
        UnionError error = new UnionError.Custom(
            "RATE_LIMIT", "Too many requests", StatusCode: 429);

        IResult httpResult = error.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(429, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_CustomErrorDefault_Returns422()
    {
        UnionError error = new UnionError.Custom("BUSINESS_RULE", "Invalid operation");

        IResult httpResult = error.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(422, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_CustomError_IncludesErrorCodeInExtensions()
    {
        UnionError error = new UnionError.Custom("RATE_LIMIT", "Too many requests");

        IResult httpResult = error.ToHttpResult();
        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal("RATE_LIMIT", problemResult.ProblemDetails.Extensions["errorCode"]);
    }

    [Fact]
    public void ToHttpResult_CustomErrorWithExtensions_IncludesAllExtensions()
    {
        var extensions = new Dictionary<string, object>
        {
            ["retryAfter"] = 30
        };

        UnionError error = new UnionError.Custom(
            "RATE_LIMIT", "Too many", Extensions: extensions);

        IResult httpResult = error.ToHttpResult();
        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal(30, problemResult.ProblemDetails.Extensions["retryAfter"]);
    }

    [Fact]
    public void ToHttpResult_CustomError_RailDelegatesToCustomMapping()
    {
        Rail<string> result = Union.Fail<string>(
            new UnionError.Custom("EXPIRED", "Token expired", StatusCode: 410));

        IResult httpResult = result.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(410, statusResult.StatusCode);
    }

    // ── ConfigureProblem callback ───────────────────────────────────────

    [Fact]
    public void ToHttpResult_WithConfigureProblem_EnrichesProblemDetails()
    {
        UnionError error = new UnionError.NotFound("Item");

        IResult httpResult = error.ToHttpResult(configureProblem: pd =>
        {
            pd.Extensions["traceId"] = "abc-123";
        });

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal("abc-123", problemResult.ProblemDetails.Extensions["traceId"]);
        Assert.Equal(StatusCodes.Status404NotFound, problemResult.ProblemDetails.Status);
    }

    [Fact]
    public void ToHttpResult_WithConfigureProblem_CanStripDetail()
    {
        UnionError error = new UnionError.SystemFailure(new Exception("secret info"));

        IResult httpResult = error.ToHttpResult(configureProblem: pd =>
        {
            pd.Detail = "An error occurred.";
        });

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal("An error occurred.", problemResult.ProblemDetails.Detail);
    }

    [Fact]
    public void ToHttpResult_WithConfigureProblem_OnValidation_EnrichesProblemDetails()
    {
        UnionError error = UnionError.CreateValidation([("Email", ["Invalid"])]);

        IResult httpResult = error.ToHttpResult(configureProblem: pd =>
        {
            pd.Extensions["traceId"] = "trace-456";
        });

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal("trace-456", problemResult.ProblemDetails.Extensions["traceId"]);
    }

    [Fact]
    public void ToHttpResult_WithConfigureProblem_OnCustom_EnrichesProblemDetails()
    {
        UnionError error = new UnionError.Custom("CODE", "msg");

        IResult httpResult = error.ToHttpResult(configureProblem: pd =>
        {
            pd.Extensions["requestId"] = "req-789";
        });

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal("req-789", problemResult.ProblemDetails.Extensions["requestId"]);
    }

    [Fact]
    public void ToHttpResult_WithoutConfigureProblem_ReturnsStandardProblem()
    {
        UnionError error = new UnionError.Conflict("dupe");

        IResult httpResult = error.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status409Conflict, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_RailWithConfigureProblem_PropagatesCallback()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.Forbidden("no access"));

        IResult httpResult = result.ToHttpResult(configureProblem: pd =>
        {
            pd.Extensions["policy"] = "admin-only";
        });

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal("admin-only", problemResult.ProblemDetails.Extensions["policy"]);
        Assert.Equal(StatusCodes.Status403Forbidden, problemResult.ProblemDetails.Status);
    }

    [Fact]
    public async Task ToHttpResultAsync_WithConfigureProblem_PropagatesCallback()
    {
        Task<Rail<string>> resultTask = Task.FromResult(
            Union.Fail<string>(new UnionError.Unauthorized()));

        IResult httpResult = await resultTask.ToHttpResultAsync(
            configureProblem: pd => pd.Extensions["hint"] = "use bearer token");

        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(httpResult);
        Assert.Equal("use bearer token", problemResult.ProblemDetails.Extensions["hint"]);
    }

    // ── CancellationToken support ───────────────────────────────────────

    [Fact]
    public async Task ToHttpResultAsync_Task_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Task<Rail<string>> resultTask = Task.FromResult(Union.Ok("hello"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => resultTask.ToHttpResultAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ToHttpResultAsync_ValueTask_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        ValueTask<Rail<string>> resultTask = new(Union.Ok("hello"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await resultTask.ToHttpResultAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ToHttpResultAsync_Task_WhenNotCancelled_ReturnsNormally()
    {
        using var cts = new CancellationTokenSource();

        Task<Rail<string>> resultTask = Task.FromResult(Union.Ok("hello"));

        IResult httpResult = await resultTask.ToHttpResultAsync(cancellationToken: cts.Token);
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }
}
