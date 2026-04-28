using BenchmarkDotNet.Attributes;

namespace UnionRailway.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class RailBenchmarks
{
    private Rail<int> _consumedResult;
    private int _consumedValue;

    // ── Creation Benchmarks ────────────────────────────────────────────

    [Benchmark(Description = "Create success Rail<int>")]
    public void CreateSuccess()
    {
        _consumedResult = Union.Ok(42);
    }

    [Benchmark(Description = "Create failure Rail<int>")]
    public void CreateFailure()
    {
        _consumedResult = Union.Fail<int>(new UnionError.NotFound("User"));
    }

    [Benchmark(Description = "Create via implicit conversion")]
    public Rail<string> CreateImplicit()
    {
        return "hello";
    }

    // ── Pattern Matching Benchmarks ────────────────────────────────────

    [Benchmark(Description = "IsSuccess pattern")]
    public void IsSuccessPattern()
    {
        var result = Union.Ok(42);
        if (result.IsSuccess(out var value, out var error))
        {
            _consumedValue = value;
        }
        else
        {
            _consumedValue = 0;
        }
    }

    [Benchmark(Description = "Match pattern")]
    public void MatchPattern()
    {
        var result = Union.Ok(42);
        _consumedValue = result.Match(
            onOk: value => value,
            onError: _ => 0);
    }

    [Benchmark(Description = "TryGetValue pattern")]
    public void TryGetValuePattern()
    {
        var result = Union.Ok(42);
        if (result.TryGetValue(out var value))
        {
            _consumedValue = value;
        }
        else
        {
            _consumedValue = 0;
        }
    }

    // ── Railway Operations Benchmarks ──────────────────────────────────

    [Benchmark(Description = "Map operation (success path)")]
    public void MapSuccess()
    {
        _consumedResult = Union.Ok(5).Map(x => x * 2);
    }

    [Benchmark(Description = "Map operation (error path)")]
    public void MapError()
    {
        var result = Union.Fail<int>(new UnionError.NotFound("User"));
        _consumedResult = result.Map(x => x * 2);
    }

    [Benchmark(Description = "Bind operation (success path)")]
    public Rail<string> BindSuccess()
    {
        return Union.Ok(42).Bind(x => Union.Ok($"value={x}"));
    }

    [Benchmark(Description = "Bind operation (error path)")]
    public Rail<string> BindError()
    {
        var result = Union.Fail<int>(new UnionError.NotFound("User"));
        return result.Bind(x => Union.Ok($"value={x}"));
    }

    [Benchmark(Description = "Chain multiple Map operations")]
    public Rail<int> ChainedMap()
    {
        return Union.Ok(1)
            .Map(x => x + 1)
            .Map(x => x * 2)
            .Map(x => x - 1);
    }

    [Benchmark(Description = "Chain multiple Bind operations")]
    public Rail<int> ChainedBind()
    {
        return Union.Ok(1)
            .Bind(x => Union.Ok(x + 1))
            .Bind(x => Union.Ok(x * 2))
            .Bind(x => Union.Ok(x - 1));
    }

    // ── Async Operations Benchmarks ────────────────────────────────────

    [Benchmark(Description = "MapAsync operation")]
    public async ValueTask<Rail<int>> MapAsyncSuccess()
    {
        return await Union.Ok(5).MapAsync(x => ValueTask.FromResult(x * 2));
    }

    [Benchmark(Description = "BindAsync operation")]
    public async ValueTask<Rail<string>> BindAsyncSuccess()
    {
        return await Union.Ok(42).BindAsync(x => ValueTask.FromResult(Union.Ok($"value={x}")));
    }

    // ── UnionError Benchmarks ──────────────────────────────────────────

    [Benchmark(Description = "Create NotFound error")]
    public UnionError CreateNotFoundError()
    {
        return new UnionError.NotFound("User");
    }

    [Benchmark(Description = "Create Validation error")]
    public UnionError CreateValidationError()
    {
        return UnionError.CreateValidation([
            ("Email", new[] { "Invalid format" }),
            ("Password", new[] { "Required", "Too short" })
        ]);
    }

    [Benchmark(Description = "Pattern match UnionError")]
    public string MatchUnionError()
    {
        var error = new UnionError.NotFound("User");
        return ((UnionError)error).Value switch
        {
            UnionError.NotFound nf => $"Not found: {nf.Resource}",
            UnionError.Conflict c => $"Conflict: {c.Reason}",
            UnionError.Unauthorized => "Unauthorized",
            UnionError.Forbidden f => $"Forbidden: {f.Reason}",
            UnionError.Validation v => $"Validation: {v.Fields.Count} errors",
            UnionError.SystemFailure sf => sf.Ex.Message,
            _ => "Unknown"
        };
    }

    // ── Real-World Scenarios ───────────────────────────────────────────

    [Benchmark(Description = "Simulate service call chain")]
    public Rail<string> ServiceCallChain()
    {
        return GetUserId()
            .Bind(id => GetUserName(id))
            .Map(name => $"Hello, {name}!");
    }

    private Rail<int> GetUserId() => Union.Ok(42);

    private Rail<string> GetUserName(int id) => Union.Ok("John Doe");

    [Benchmark(Description = "Error handling scenario")]
    public Rail<string> ErrorHandlingScenario()
    {
        var result = GetUserIdWithError();
        if (!result.IsSuccess(out var id, out var error))
        {
            return error.GetValueOrDefault();
        }
        return Union.Ok($"User ID: {id}");
    }

    private Rail<int> GetUserIdWithError() =>
        Union.Fail<int>(new UnionError.NotFound("User"));
}
