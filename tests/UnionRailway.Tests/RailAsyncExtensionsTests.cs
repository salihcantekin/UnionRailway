using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using UnionRailway;
using UnionRailway.AspNetCore;

namespace UnionRailway.Tests;

public sealed class RailAsyncExtensionsTests
{
    private static TError AssertError<T, TError>(Rail<T> result)
        where TError : class
    {
        Assert.True(result.TryGetError(out UnionError? error));
        return Assert.IsType<TError>(error.GetValueOrDefault().Value);
    }

    [Fact]
    public async Task TaskRail_MapAsync_WithSyncMapper_TransformsValue()
    {
        Task<Rail<int>> resultTask = Task.FromResult(Union.Ok(21));

        Rail<int> result = await resultTask.MapAsync(x => x * 2);

        Assert.Equal(42, result.Unwrap());
    }

    [Fact]
    public async Task TaskRail_BindAsync_WithAsyncBinder_TransformsValue()
    {
        Task<Rail<int>> resultTask = Task.FromResult(Union.Ok(10));

        Rail<string> result = await resultTask.BindAsync(x => Task.FromResult<Rail<string>>(Union.Ok($"value={x}")));

        Assert.Equal("value=10", result.Unwrap());
    }

    [Fact]
    public async Task TaskRail_BindAsync_WhenError_PropagatesError()
    {
        Task<Rail<int>> resultTask = Task.FromResult(Union.Fail<int>(new UnionError.Conflict("dupe")));

        Rail<string> result = await resultTask.BindAsync(x => Task.FromResult<Rail<string>>(Union.Ok($"value={x}")));

        AssertError<string, UnionError.Conflict>(result);
    }

    [Fact]
    public async Task ValueTaskRail_MatchAsync_WithAsyncDelegates_UsesSuccessBranch()
    {
        ValueTask<Rail<int>> resultTask = ValueTask.FromResult<Rail<int>>(Union.Ok(7));

        var result = await resultTask.MatchAsync(
            onOk: x => ValueTask.FromResult($"ok:{x}"),
            onError: error => ValueTask.FromResult($"err:{error}"));

        Assert.Equal("ok:7", result);
    }

    [Fact]
    public async Task ValueTaskRail_TapAsync_InvokesCallback()
    {
        ValueTask<Rail<string>> resultTask = ValueTask.FromResult<Rail<string>>(Union.Ok("hello"));
        var called = false;

        Rail<string> result = await resultTask.TapAsync(value =>
        {
            called = value == "hello";
            return ValueTask.CompletedTask;
        });

        Assert.True(called);
        Assert.Equal("hello", result.Unwrap());
    }

    [Fact]
    public async Task TaskRail_UnwrapAsync_ReturnsValue()
    {
        Task<Rail<int>> resultTask = Task.FromResult(Union.Ok(99));

        var value = await resultTask.UnwrapAsync();

        Assert.Equal(99, value);
    }

    [Fact]
    public async Task TaskRail_ToHttpResultAsync_ReturnsOkResult()
    {
        Task<Rail<object>> resultTask = Task.FromResult<Rail<object>>(Union.Ok(new { Name = "Alice" }));

        IResult httpResult = await resultTask.ToHttpResultAsync();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    [Fact]
    public async Task ValueTaskRail_ToHttpResultAsync_ReturnsErrorMapping()
    {
        ValueTask<Rail<string>> resultTask = ValueTask.FromResult<Rail<string>>(Union.Fail<string>(new UnionError.NotFound("User")));

        IResult httpResult = await resultTask.ToHttpResultAsync();
        IStatusCodeHttpResult statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }
}
