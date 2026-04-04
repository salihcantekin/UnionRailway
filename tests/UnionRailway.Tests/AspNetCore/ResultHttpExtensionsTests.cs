using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using UnionRailway.AspNetCore;

namespace UnionRailway.Tests.AspNetCore;

public record TestPayload(int Id, string Name);

public class ResultHttpExtensionsTests
{
    [Fact]
    public void ToHttpResult_ShouldReturnOk_WhenResultIsOk()
    {
        var payload = new TestPayload(1, "Alice");
        Result<TestPayload> result = new Result<TestPayload>.Ok(payload);

        var httpResult = result.ToHttpResult();

        var okResult = httpResult.Should().BeOfType<Ok<TestPayload>>().Subject;
        okResult.Value.Should().Be(payload);
    }

    [Fact]
    public void ToHttpResult_ShouldReturnNotFound_WhenErrorIsNotFound()
    {
        Result<TestPayload> result = new Result<TestPayload>.Error(new UnionError.NotFound("User"));

        var httpResult = result.ToHttpResult();

        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)httpResult;
        statusResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ToHttpResult_ShouldReturnConflict_WhenErrorIsConflict()
    {
        Result<TestPayload> result = new Result<TestPayload>.Error(
            new UnionError.Conflict("Duplicate email"));

        var httpResult = result.ToHttpResult();

        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)httpResult;
        statusResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public void ToHttpResult_ShouldReturnUnauthorized_WhenErrorIsUnauthorized()
    {
        Result<TestPayload> result = new Result<TestPayload>.Error(new UnionError.Unauthorized());

        var httpResult = result.ToHttpResult();

        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)httpResult;
        statusResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public void ToHttpResult_ShouldReturnBadRequest_WhenErrorIsValidation()
    {
        var fields = new Dictionary<string, string>
        {
            ["Name"] = "Name is required",
            ["Email"] = "Invalid email format"
        };
        Result<TestPayload> result = new Result<TestPayload>.Error(new UnionError.Validation(fields));

        var httpResult = result.ToHttpResult();

        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        var statusResult = (IStatusCodeHttpResult)httpResult;
        statusResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ToHttpResult_ShouldReturnProblem_WhenErrorIsSystemFailure()
    {
        var exception = new InvalidOperationException("Database timeout");
        Result<TestPayload> result = new Result<TestPayload>.Error(
            new UnionError.SystemFailure(exception));

        var httpResult = result.ToHttpResult();

        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public void ToHttpResult_ShouldWorkWithValueTypes()
    {
        Result<int> result = new Result<int>.Ok(42);

        var httpResult = result.ToHttpResult();

        var okResult = httpResult.Should().BeOfType<Ok<int>>().Subject;
        okResult.Value.Should().Be(42);
    }

    [Fact]
    public void ToHttpResult_ShouldWorkWithStringType()
    {
        Result<string> result = new Result<string>.Ok("success");

        var httpResult = result.ToHttpResult();

        var okResult = httpResult.Should().BeOfType<Ok<string>>().Subject;
        okResult.Value.Should().Be("success");
    }

    [Fact]
    public void ToHttpResult_ShouldWorkWithListType()
    {
        var data = new List<string> { "a", "b" };
        Result<List<string>> result = new Result<List<string>>.Ok(data);

        var httpResult = result.ToHttpResult();

        var okResult = httpResult.Should().BeOfType<Ok<List<string>>>().Subject;
        okResult.Value.Should().HaveCount(2);
    }

    [Fact]
    public void UnionError_ToHttpResult_ShouldMapNotFoundCorrectly()
    {
        var error = new UnionError.NotFound("Order") as UnionError;

        var httpResult = error.ToHttpResult();

        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)httpResult).StatusCode.Should().Be(404);
    }

    [Fact]
    public void UnionError_ToHttpResult_ShouldMapUnauthorizedCorrectly()
    {
        var error = new UnionError.Unauthorized() as UnionError;

        var httpResult = error.ToHttpResult();

        httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)httpResult).StatusCode.Should().Be(401);
    }

    [Fact]
    public void ToHttpResult_WithIsSuccess_IntegrationPattern()
    {
        Result<TestPayload> result = new Result<TestPayload>.Error(new UnionError.NotFound("User"));

        if (!result.IsSuccess(out var data, out var error))
        {
            var httpResult = error!.ToHttpResult();
            httpResult.Should().BeAssignableTo<IStatusCodeHttpResult>();
            ((IStatusCodeHttpResult)httpResult).StatusCode.Should().Be(404);
            return;
        }

        Assert.Fail("Should have returned early.");
    }
}
