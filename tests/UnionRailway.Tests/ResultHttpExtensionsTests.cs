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

        IResult httpResult    = result.ToHttpResult();
        IStatusCodeHttpResult statusResult  = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_OkTuple_WithCreatedUri_Returns201()
    {
        var result = Union.Ok(new { Id = 1 });

        IResult httpResult   = result.ToHttpResult(createdUri: "/items/1");
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
    }

    // ── UnionError → IResult ─────────────────────────────────────────

    [Fact]
    public void ToHttpResult_NotFound_Returns404()
    {
        UnionError error = new UnionError.NotFound("Item");
        IResult httpResult   = error.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_Conflict_Returns409()
    {
        UnionError error = new UnionError.Conflict("Dupe");
        IResult httpResult   = error.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status409Conflict, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_Unauthorized_Returns401()
    {
        UnionError error = new UnionError.Unauthorized();
        IResult httpResult   = error.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_Forbidden_Returns403()
    {
        UnionError error = new UnionError.Forbidden("No access");
        IResult httpResult   = error.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_Validation_Returns400()
    {
        IResult httpResult   = UnionError.CreateValidation([("Email", ["Invalid format"])]).ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_SystemFailure_Returns500()
    {
        UnionError error = new UnionError.SystemFailure(new Exception("boom"));
        IResult httpResult   = error.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public void ToHttpResult_ErrorTuple_DelegatesToErrorMapping()
    {
        Rail<string> result = Union.Fail<string>(new UnionError.NotFound("Order"));

        IResult httpResult   = result.ToHttpResult();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }
}
