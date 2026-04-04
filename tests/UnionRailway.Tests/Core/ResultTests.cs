using FluentAssertions;

namespace UnionRailway.Tests.Core;

public class ResultTests
{
    [Fact]
    public void Ok_ShouldStoreData()
    {
        var result = new Result<int>.Ok(42);
        result.Data.Should().Be(42);
    }

    [Fact]
    public void Error_ShouldStoreUnionError()
    {
        var error = new UnionError.NotFound("Product");
        var result = new Result<int>.Error(error);
        result.Err.Should().Be(error);
    }

    [Fact]
    public void PatternMatching_ShouldDistinguishOkAndError()
    {
        Result<string> okResult = new Result<string>.Ok("Hello");
        Result<string> errorResult = new Result<string>.Error(new UnionError.Unauthorized());

        var okMessage = okResult switch
        {
            Result<string>.Ok(var data) => $"Success: {data}",
            Result<string>.Error(var err) => $"Failed: {err}",
            _ => "Unknown"
        };

        var errorMessage = errorResult switch
        {
            Result<string>.Ok(var data) => $"Success: {data}",
            Result<string>.Error(var err) => $"Failed: {err}",
            _ => "Unknown"
        };

        okMessage.Should().Be("Success: Hello");
        errorMessage.Should().Contain("Unauthorized");
    }

    [Fact]
    public void Result_ShouldSupportReferenceTypes()
    {
        var result = new Result<List<string>>.Ok(["a", "b", "c"]);
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public void Result_ShouldSupportValueTypes()
    {
        var result = new Result<double>.Ok(3.14);
        result.Data.Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void Ok_EqualityByValue()
    {
        var result1 = new Result<int>.Ok(10);
        var result2 = new Result<int>.Ok(10);
        var result3 = new Result<int>.Ok(20);

        result1.Should().Be(result2);
        result1.Should().NotBe(result3);
    }

    [Fact]
    public void Error_EqualityByValue()
    {
        var error = new UnionError.NotFound("User");
        var result1 = new Result<int>.Error(error);
        var result2 = new Result<int>.Error(new UnionError.NotFound("User"));

        result1.Should().Be(result2);
    }

    [Fact]
    public void NestedPatternMatching_ShouldWorkWithErrorVariants()
    {
        Result<string> result = new Result<string>.Error(
            new UnionError.Validation(new Dictionary<string, string>
            {
                ["Email"] = "Required"
            }));

        var message = result switch
        {
            Result<string>.Ok(var data) => data,
            Result<string>.Error(UnionError.Validation(var fields)) => fields["Email"],
            Result<string>.Error => "Other error",
            _ => "Unknown"
        };

        message.Should().Be("Required");
    }
}
