using UnionRailway;

namespace UnionRailway.Tests;

public sealed class UnionErrorTests
{
    // ── Direct construction ─────────────────────────────────────────────────

    [Fact]
    public void NotFound_CreatesCorrectType()
    {
        var error = new UnionError.NotFound("User");

        UnionError.NotFound nf = Assert.IsType<UnionError.NotFound>(error);
        Assert.Equal("User", nf.Resource);
    }

    [Fact]
    public void Conflict_CreatesCorrectType()
    {
        var error = new UnionError.Conflict("Duplicate email");

        UnionError.Conflict c = Assert.IsType<UnionError.Conflict>(error);
        Assert.Equal("Duplicate email", c.Reason);
    }

    [Fact]
    public void Unauthorized_CreatesCorrectType()
    {
        var error = new UnionError.Unauthorized();

        Assert.IsType<UnionError.Unauthorized>(error);
    }

    [Fact]
    public void Forbidden_CreatesCorrectType()
    {
        var error = new UnionError.Forbidden("Admin only");

        UnionError.Forbidden f = Assert.IsType<UnionError.Forbidden>(error);
        Assert.Equal("Admin only", f.Reason);
    }

    [Fact]
    public void Validation_FromDictionary_CreatesCorrectType()
    {
        var fields = new Dictionary<string, string[]>
        {
            ["Email"] = ["Invalid format"],
            ["Name"]  = ["Required"]
        };
        var error = new UnionError.Validation(fields);

        UnionError.Validation v = Assert.IsType<UnionError.Validation>(error);
        Assert.Contains("Email", v.Fields.Keys);
        Assert.Contains("Name",  v.Fields.Keys);
    }

    [Fact]
    public void Validation_FromPairs_CreatesCorrectType()
    {
        var error = UnionError.CreateValidation(
        [
            ("Email", ["Invalid format"]),
            ("Name",  ["Required"])
        ]);

        UnionError.Validation v = Assert.IsType<UnionError.Validation>(error.Value);
        Assert.Contains("Email", v.Fields.Keys);
        Assert.Contains("Name",  v.Fields.Keys);
    }

    [Fact]
    public void SystemFailure_CreatesCorrectType()
    {
        var ex    = new InvalidOperationException("boom");
        var error = new UnionError.SystemFailure(ex);

        UnionError.SystemFailure sf = Assert.IsType<UnionError.SystemFailure>(error);
        Assert.Same(ex, sf.Ex);
    }

    // ── Pattern matching ──────────────────────────────────────────────

    [Fact]
    public void SwitchExpression_MatchesCorrectly()
    {
        UnionError error = new UnionError.NotFound("Item");

        var message = error.Value switch
        {
            UnionError.NotFound nf      => $"not found: {nf.Resource}",
            UnionError.Conflict c       => $"conflict: {c.Reason}",
            UnionError.Unauthorized     => "unauthorized",
            UnionError.Forbidden f      => $"forbidden: {f.Reason}",
            UnionError.Validation v     => $"validation ({v.Fields.Count} fields)",
            UnionError.SystemFailure sf => $"system: {sf.Ex.Message}",
            _                           => "other"
        };

        Assert.Equal("not found: Item", message);
    }

    // ── Record equality ───────────────────────────────────────────────

    [Fact]
    public void NotFound_SameResource_AreEqual()
    {
        var a = new UnionError.NotFound("X");
        var b = new UnionError.NotFound("X");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NotFound_DifferentResource_AreNotEqual()
    {
        var a = new UnionError.NotFound("X");
        var b = new UnionError.NotFound("Y");

        Assert.NotEqual(a, b);
    }

    // ── Sealed hierarchy ─────────────────────────────────────────────

    [Fact]
    public void AllCasesCanConvertToUnionError()
    {
        UnionError notFound = new UnionError.NotFound("r");
        UnionError conflict = new UnionError.Conflict("r");
        UnionError unauthorized = new UnionError.Unauthorized();
        UnionError forbidden = new UnionError.Forbidden("r");
        UnionError validation = new UnionError.Validation(new Dictionary<string, string[]>());
        UnionError failure = new UnionError.SystemFailure(new Exception());

        Assert.IsType<UnionError.NotFound>(notFound.Value);
        Assert.IsType<UnionError.Conflict>(conflict.Value);
        Assert.IsType<UnionError.Unauthorized>(unauthorized.Value);
        Assert.IsType<UnionError.Forbidden>(forbidden.Value);
        Assert.IsType<UnionError.Validation>(validation.Value);
        Assert.IsType<UnionError.SystemFailure>(failure.Value);
    }
}
