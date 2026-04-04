using FluentAssertions;

namespace UnionRailway.Tests.Core;

public class ResultExtensionsTests
{
    [Fact]
    public void Map_ShouldTransformOkValue()
    {
        Result<int> result = new Result<int>.Ok(5);
        var mapped = result.Map(x => x * 2);
        mapped.Should().Be(new Result<int>.Ok(10));
    }

    [Fact]
    public void Map_ShouldPropagateError()
    {
        var error = new UnionError.NotFound("Item");
        Result<int> result = new Result<int>.Error(error);
        var mapped = result.Map(x => x * 2);
        mapped.Should().Be(new Result<int>.Error(error));
    }

    [Fact]
    public void Bind_ShouldChainOnSuccess()
    {
        Result<int> result = new Result<int>.Ok(10);
        var bound = result.Bind<int, string>(x => x > 0
            ? new Result<string>.Ok($"Positive: {x}")
            : new Result<string>.Error(new UnionError.Validation(new Dictionary<string, string>
            {
                ["Value"] = "Must be positive"
            })));

        bound.Should().Be(new Result<string>.Ok("Positive: 10"));
    }

    [Fact]
    public void Bind_ShouldPropagateError()
    {
        var error = new UnionError.Unauthorized();
        Result<int> result = new Result<int>.Error(error);
        var bound = result.Bind(x => new Result<string>.Ok(x.ToString()));
        bound.Should().Be(new Result<string>.Error(error));
    }

    [Fact]
    public void Match_ShouldApplyOnOkForSuccess()
    {
        Result<int> result = new Result<int>.Ok(42);
        var output = result.Match(
            onOk: x => $"Value is {x}",
            onError: e => $"Error: {e}");

        output.Should().Be("Value is 42");
    }

    [Fact]
    public void Match_ShouldApplyOnErrorForFailure()
    {
        Result<int> result = new Result<int>.Error(new UnionError.NotFound("Order"));
        var output = result.Match(
            onOk: x => "Ok",
            onError: e => e switch
            {
                UnionError.NotFound(var r) => $"Not found: {r}",
                _ => "Other error"
            });

        output.Should().Be("Not found: Order");
    }

    [Fact]
    public void Tap_ShouldExecuteActionOnSuccess()
    {
        var sideEffect = 0;
        Result<int> result = new Result<int>.Ok(5);
        var tapped = result.Tap(x => sideEffect = x);

        sideEffect.Should().Be(5);
        tapped.Should().Be(result);
    }

    [Fact]
    public void Tap_ShouldNotExecuteActionOnError()
    {
        var sideEffect = 0;
        Result<int> result = new Result<int>.Error(new UnionError.Unauthorized());
        var tapped = result.Tap(x => sideEffect = x);

        sideEffect.Should().Be(0);
        tapped.Should().Be(result);
    }

    [Fact]
    public void MapError_ShouldTransformError()
    {
        Result<int> result = new Result<int>.Error(new UnionError.NotFound("X"));
        var mapped = result.MapError(e => new UnionError.Conflict("Wrapped error"));

        mapped.Should().BeOfType<Result<int>.Error>();
        var error = (Result<int>.Error)mapped;
        error.Err.Should().BeOfType<UnionError.Conflict>();
    }

    [Fact]
    public void MapError_ShouldNotAffectOk()
    {
        Result<int> result = new Result<int>.Ok(42);
        var mapped = result.MapError(e => new UnionError.Conflict("Should not reach"));
        mapped.Should().Be(result);
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
    public async Task MapAsync_ShouldTransformOkValue()
    {
        var resultTask = Task.FromResult<Result<int>>(new Result<int>.Ok(3));
        var mapped = await resultTask.MapAsync(x => x + 1);
        mapped.Should().Be(new Result<int>.Ok(4));
    }

    [Fact]
    public async Task MapAsync_ShouldPropagateError()
    {
        var error = new UnionError.SystemFailure(new Exception("fail"));
        var resultTask = Task.FromResult<Result<int>>(new Result<int>.Error(error));
        var mapped = await resultTask.MapAsync(x => x + 1);
        mapped.Should().Be(new Result<int>.Error(error));
    }

    [Fact]
    public async Task BindAsync_ShouldChainOnSuccess()
    {
        var resultTask = Task.FromResult<Result<int>>(new Result<int>.Ok(5));
        var bound = await resultTask.BindAsync(x => new Result<string>.Ok(x.ToString()));
        bound.Should().Be(new Result<string>.Ok("5"));
    }

    [Fact]
    public async Task BindAsync_WithAsyncBinder_ShouldChainOnSuccess()
    {
        var resultTask = Task.FromResult<Result<int>>(new Result<int>.Ok(5));
        var bound = await resultTask.BindAsync(x => Task.FromResult<Result<string>>(new Result<string>.Ok(x.ToString())));
        bound.Should().Be(new Result<string>.Ok("5"));
    }

    [Fact]
    public async Task BindAsync_WithAsyncBinder_ShouldPropagateError()
    {
        var error = new UnionError.Unauthorized();
        var resultTask = Task.FromResult<Result<int>>(new Result<int>.Error(error));
        var bound = await resultTask.BindAsync(x => Task.FromResult<Result<string>>(new Result<string>.Ok(x.ToString())));
        bound.Should().Be(new Result<string>.Error(error));
    }

    [Fact]
    public async Task MatchAsync_ShouldReturnCorrectValue()
    {
        var resultTask = Task.FromResult<Result<int>>(new Result<int>.Ok(7));
        var output = await resultTask.MatchAsync(x => x * 10, _ => -1);
        output.Should().Be(70);
    }

    [Fact]
    public async Task TapAsync_ShouldExecuteActionOnSuccess()
    {
        var sideEffect = 0;
        var resultTask = Task.FromResult<Result<int>>(new Result<int>.Ok(9));
        var result = await resultTask.TapAsync(x => sideEffect = x);

        sideEffect.Should().Be(9);
        result.Should().Be(new Result<int>.Ok(9));
    }

    [Fact]
    public void Chaining_MapThenBind_ShouldComposeCorrectly()
    {
        Result<int> result = new Result<int>.Ok(5);
        var chained = result
            .Map(x => x * 2)
            .Bind<int, string>(x => x > 5
                ? new Result<string>.Ok($"Big: {x}")
                : new Result<string>.Error(new UnionError.Validation(new Dictionary<string, string>
                {
                    ["Value"] = "Too small"
                })));

        chained.Should().Be(new Result<string>.Ok("Big: 10"));
    }

    [Fact]
    public void Chaining_MapThenBind_ShouldShortCircuitOnError()
    {
        Result<int> result = new Result<int>.Error(new UnionError.NotFound("X"));
        var chained = result
            .Map(x => x * 2)
            .Bind(x => new Result<string>.Ok(x.ToString()));

        chained.Should().BeOfType<Result<string>.Error>();
    }
}
