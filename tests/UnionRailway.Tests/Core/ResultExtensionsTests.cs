using FluentAssertions;

namespace UnionRailway.Tests.Core;

public class ResultExtensionsTests
{
    [Fact]
    public void IsSuccess_ShouldReturnTrue_WhenResultIsOk()
    {
        Result<int> result = new Result<int>.Ok(42);

        var success = result.IsSuccess(out var data, out var error);

        success.Should().BeTrue();
        data.Should().Be(42);
        error.Should().BeNull();
    }

    [Fact]
    public void IsSuccess_ShouldReturnFalse_WhenResultIsError()
    {
        var notFound = new UnionError.NotFound("User");
        Result<int> result = new Result<int>.Error(notFound);

        var success = result.IsSuccess(out var data, out var error);

        success.Should().BeFalse();
        data.Should().Be(0);
        error.Should().Be(notFound);
    }

    [Fact]
    public void IsSuccess_ShouldSupportEarlyReturn_Pattern()
    {
        Result<string> result = new Result<string>.Error(new UnionError.Unauthorized());

        if (!result.IsSuccess(out var data, out var error))
        {
            error.Should().BeOfType<UnionError.Unauthorized>();
            data.Should().BeNull();
            return;
        }

        Assert.Fail("Should have returned early.");
    }

    [Fact]
    public void IsSuccess_ShouldWorkWithReferenceTypes()
    {
        Result<string> result = new Result<string>.Ok("hello");

        var success = result.IsSuccess(out var data, out var error);

        success.Should().BeTrue();
        data.Should().Be("hello");
        error.Should().BeNull();
    }

    [Fact]
    public void IsSuccess_ShouldWorkWithNullableReferenceTypes()
    {
        Result<string> result = new Result<string>.Error(new UnionError.NotFound("Item"));

        var success = result.IsSuccess(out var data, out var error);

        success.Should().BeFalse();
        data.Should().BeNull();
        error.Should().BeOfType<UnionError.NotFound>();
    }

    [Fact]
    public void IsSuccess_ShouldWorkWithComplexTypes()
    {
        var list = new List<string> { "a", "b", "c" };
        Result<List<string>> result = new Result<List<string>>.Ok(list);

        var success = result.IsSuccess(out var data, out var error);

        success.Should().BeTrue();
        data.Should().BeSameAs(list);
        data.Should().HaveCount(3);
        error.Should().BeNull();
    }

    [Fact]
    public async Task IsSuccessAsync_ShouldReturnTrue_WhenResultIsOk()
    {
        var resultTask = Task.FromResult<Result<int>>(new Result<int>.Ok(99));

        var (success, data, error) = await resultTask.IsSuccessAsync();

        success.Should().BeTrue();
        data.Should().Be(99);
        error.Should().BeNull();
    }

    [Fact]
    public async Task IsSuccessAsync_ShouldReturnFalse_WhenResultIsError()
    {
        var conflict = new UnionError.Conflict("Duplicate record");
        var resultTask = Task.FromResult<Result<string>>(new Result<string>.Error(conflict));

        var (success, data, error) = await resultTask.IsSuccessAsync();

        success.Should().BeFalse();
        data.Should().BeNull();
        error.Should().Be(conflict);
    }

    [Fact]
    public void ToResult_ShouldReturnOkForNonNullReference()
    {
        string? value = "hello";
        var result = value.ToResult("String");
        result.Should().Be(new Result<string>.Ok("hello"));
    }

    [Fact]
    public void ToResult_ShouldReturnNotFoundForNullReference()
    {
        string? value = null;
        var result = value.ToResult("String");
        result.Should().Be(new Result<string>.Error(new UnionError.NotFound("String")));
    }

    [Fact]
    public void ToResult_ShouldReturnOkForNonNullValueType()
    {
        int? value = 42;
        var result = value.ToResult("Int");
        result.Should().Be(new Result<int>.Ok(42));
    }

    [Fact]
    public void ToResult_ShouldReturnNotFoundForNullValueType()
    {
        int? value = null;
        var result = value.ToResult("Int");
        result.Should().Be(new Result<int>.Error(new UnionError.NotFound("Int")));
    }

    [Fact]
    public void IsSuccess_ShouldWorkInChainedScenario()
    {
        var step1 = GetUser(1);
        if (!step1.IsSuccess(out var user, out var error))
        {
            Assert.Fail("Step 1 should succeed.");
            return;
        }

        user.Should().Be("Alice");

        var step2 = GetUser(999);
        if (!step2.IsSuccess(out var missingUser, out var missingError))
        {
            missingError.Should().BeOfType<UnionError.NotFound>();
            return;
        }

        Assert.Fail("Step 2 should have returned early.");
    }

    [Fact]
    public void IsSuccess_ShouldHandleAllErrorVariants()
    {
        UnionError[] errors =
        [
            new UnionError.NotFound("X"),
            new UnionError.Conflict("Y"),
            new UnionError.Unauthorized(),
            new UnionError.Validation(new Dictionary<string, string> { ["field"] = "error" }),
            new UnionError.SystemFailure(new Exception("boom"))
        ];

        foreach (var err in errors)
        {
            Result<int> result = new Result<int>.Error(err);
            var success = result.IsSuccess(out var data, out var error);

            success.Should().BeFalse();
            error.Should().Be(err);
        }
    }

    private static Result<string> GetUser(int id) =>
        id switch
        {
            1 => new Result<string>.Ok("Alice"),
            _ => new Result<string>.Error(new UnionError.NotFound("User"))
        };
}
