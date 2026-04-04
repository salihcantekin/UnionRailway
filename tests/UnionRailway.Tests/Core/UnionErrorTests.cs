using FluentAssertions;

namespace UnionRailway.Tests.Core;

public class UnionErrorTests
{
    [Fact]
    public void NotFound_ShouldStoreResource()
    {
        var error = new UnionError.NotFound("User");
        error.Resource.Should().Be("User");
    }

    [Fact]
    public void Conflict_ShouldStoreReason()
    {
        var error = new UnionError.Conflict("Duplicate email");
        error.Reason.Should().Be("Duplicate email");
    }

    [Fact]
    public void Unauthorized_ShouldBeCreatable()
    {
        var error = new UnionError.Unauthorized();
        error.Should().BeOfType<UnionError.Unauthorized>();
    }

    [Fact]
    public void Validation_ShouldStoreFields()
    {
        var fields = new Dictionary<string, string>
        {
            ["Name"] = "Name is required",
            ["Email"] = "Invalid email"
        };

        var error = new UnionError.Validation(fields);
        error.Fields.Should().HaveCount(2);
        error.Fields["Name"].Should().Be("Name is required");
    }

    [Fact]
    public void SystemFailure_ShouldStoreException()
    {
        var exception = new InvalidOperationException("Something broke");
        var error = new UnionError.SystemFailure(exception);
        error.Ex.Should().BeSameAs(exception);
        error.Ex.Message.Should().Be("Something broke");
    }

    [Fact]
    public void PatternMatching_ShouldMatchAllVariants()
    {
        UnionError[] errors =
        [
            new UnionError.NotFound("Order"),
            new UnionError.Conflict("Duplicate"),
            new UnionError.Unauthorized(),
            new UnionError.Validation(new Dictionary<string, string>()),
            new UnionError.SystemFailure(new Exception())
        ];

        var messages = errors.Select(e => e switch
        {
            UnionError.NotFound(var resource) => $"NotFound:{resource}",
            UnionError.Conflict(var reason) => $"Conflict:{reason}",
            UnionError.Unauthorized => "Unauthorized",
            UnionError.Validation(var fields) => $"Validation:{fields.Count}",
            UnionError.SystemFailure(var ex) => $"SystemFailure:{ex.GetType().Name}",
            _ => "Unknown"
        }).ToList();

        messages.Should().Equal(
        [
            "NotFound:Order",
            "Conflict:Duplicate",
            "Unauthorized",
            "Validation:0",
            "SystemFailure:Exception"
        ]);
    }

    [Fact]
    public void EqualityByValue_ShouldWorkForRecords()
    {
        var error1 = new UnionError.NotFound("User");
        var error2 = new UnionError.NotFound("User");
        var error3 = new UnionError.NotFound("Order");

        error1.Should().Be(error2);
        error1.Should().NotBe(error3);
    }
}
