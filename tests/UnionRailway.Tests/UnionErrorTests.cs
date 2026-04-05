using UnionRailway;

namespace UnionRailway.Tests;

public sealed class UnionErrorTests
{
    // ── Direct construction ─────────────────────────────────────────────────

    [Fact]
    public void NotFound_CreatesCorrectType()
    {
        var error = new UnionError.NotFound("User");

        var nf = Assert.IsType<UnionError.NotFound>(error);
        Assert.Equal("User", nf.Resource);
    }

    [Fact]
    public void Conflict_CreatesCorrectType()
    {
        var error = new UnionError.Conflict("Duplicate email");

        var c = Assert.IsType<UnionError.Conflict>(error);
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

        var f = Assert.IsType<UnionError.Forbidden>(error);
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

        var v = Assert.IsType<UnionError.Validation>(error);
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

        var v = Assert.IsType<UnionError.Validation>(error);
        Assert.Contains("Email", v.Fields.Keys);
        Assert.Contains("Name",  v.Fields.Keys);
    }

    [Fact]
    public void SystemFailure_CreatesCorrectType()
    {
        var ex    = new InvalidOperationException("boom");
        var error = new UnionError.SystemFailure(ex);

        var sf = Assert.IsType<UnionError.SystemFailure>(error);
        Assert.Same(ex, sf.Ex);
    }

    // ── Pattern matching ──────────────────────────────────────────────

    [Fact]
    public void SwitchExpression_MatchesCorrectly()
    {
        UnionError error = new UnionError.NotFound("Item");

        var message = error switch
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
    public void AllCasesAreSubtypesOfUnionError()
    {
        Assert.IsAssignableFrom<UnionError>(new UnionError.NotFound("r"));
        Assert.IsAssignableFrom<UnionError>(new UnionError.Conflict("r"));
        Assert.IsAssignableFrom<UnionError>(new UnionError.Unauthorized());
        Assert.IsAssignableFrom<UnionError>(new UnionError.Forbidden("r"));
        Assert.IsAssignableFrom<UnionError>(
            new UnionError.Validation(new Dictionary<string, string[]>()));
        Assert.IsAssignableFrom<UnionError>(
            new UnionError.SystemFailure(new Exception()));
    }
}
