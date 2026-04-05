using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using UnionRailway;
using UnionRailway.AspNetCore;

namespace UnionRailway.Tests;

public sealed class ResultHttpExtensionsTests
{
    // ── Rail → IResult ───────────────────────────────────────────────

    [Fact]
    public void ToHttpResult_OkTuple_Returns200()
    {
        var result = Union.Ok(new { Name = "Alice" });

        var httpResult    = result.ToHttpResult();
        var statusResult  = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_OkTuple_WithCreatedUri_Returns201()
    {
        var result = Union.Ok(new { Id = 1 });

        var httpResult   = result.ToHttpResult(createdUri: "/items/1");
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
    }

    // ── UnionError → IResult ─────────────────────────────────────────

    [Fact]
    public void ToHttpResult_NotFound_Returns404()
    {
        UnionError error = new UnionError.NotFound("Item");
        var httpResult   = error.ToHttpResult();
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_Conflict_Returns409()
    {
        UnionError error = new UnionError.Conflict("Dupe");
        var httpResult   = error.ToHttpResult();
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status409Conflict, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_Unauthorized_Returns401()
    {
        UnionError error = new UnionError.Unauthorized();
        var httpResult   = error.ToHttpResult();
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_Forbidden_Returns403()
    {
        UnionError error = new UnionError.Forbidden("No access");
        var httpResult   = error.ToHttpResult();
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_Validation_Returns400()
    {
        var httpResult   = UnionError.CreateValidation([("Email", ["Invalid format"])]).ToHttpResult();
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_SystemFailure_Returns500()
    {
        UnionError error = new UnionError.SystemFailure(new Exception("boom"));
        var httpResult   = error.ToHttpResult();
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_ErrorTuple_DelegatesToErrorMapping()
    {
        var result = Union.Fail<string>(new UnionError.NotFound("Order"));

        var httpResult   = result.ToHttpResult();
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }
}
