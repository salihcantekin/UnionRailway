using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UnionRailway.EntityFrameworkCore;

namespace UnionRailway.Tests.EntityFrameworkCore;

public class DbContextExtensionsTests : IDisposable
{
    private readonly TestDbContext context;

    public DbContextExtensionsTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        this.context = new TestDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsUnionAsync_ShouldReturnOk_WhenSaveSucceeds()
    {
        this.context.TestEntities.Add(new TestEntity { Id = 1, Name = "Alice", Email = "alice@test.com" });
        var result = await this.context.SaveChangesAsUnionAsync();

        result.Should().BeOfType<Result<int>.Ok>();
        var ok = (Result<int>.Ok)result;
        ok.Data.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsUnionAsync_ShouldReturnOk_WithCorrectCount()
    {
        this.context.TestEntities.AddRange(
        [
            new TestEntity { Id = 10, Name = "X", Email = "x@test.com" },
            new TestEntity { Id = 11, Name = "Y", Email = "y@test.com" },
            new TestEntity { Id = 12, Name = "Z", Email = "z@test.com" }
        ]);
        var result = await this.context.SaveChangesAsUnionAsync();

        result.Should().BeOfType<Result<int>.Ok>();
        var ok = (Result<int>.Ok)result;
        ok.Data.Should().Be(3);
    }

    [Fact]
    public async Task FindAsUnionAsync_ShouldReturnOk_WhenEntityExists()
    {
        this.context.TestEntities.Add(new TestEntity { Id = 1, Name = "Alice", Email = "alice@test.com" });
        await this.context.SaveChangesAsync();

        var result = await this.context.FindAsUnionAsync<TestEntity>(1);

        result.Should().BeOfType<Result<TestEntity>.Ok>();
        var ok = (Result<TestEntity>.Ok)result;
        ok.Data.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task FindAsUnionAsync_ShouldReturnNotFound_WhenEntityDoesNotExist()
    {
        var result = await this.context.FindAsUnionAsync<TestEntity>(999);

        result.Should().BeOfType<Result<TestEntity>.Error>();
        var error = (Result<TestEntity>.Error)result;
        error.Err.Should().BeOfType<UnionError.NotFound>();
        ((UnionError.NotFound)error.Err).Resource.Should().Be("TestEntity");
    }

    [Fact]
    public async Task FindAsUnionAsync_ShouldReturnNotFound_ForNonExistentKey()
    {
        this.context.TestEntities.Add(new TestEntity { Id = 1, Name = "Only", Email = "only@test.com" });
        await this.context.SaveChangesAsync();

        var result = await this.context.FindAsUnionAsync<TestEntity>(42);

        result.Should().BeOfType<Result<TestEntity>.Error>();
        var error = (Result<TestEntity>.Error)result;
        error.Err.Should().BeOfType<UnionError.NotFound>();
    }

    public void Dispose()
    {
        this.context.Dispose();
        GC.SuppressFinalize(this);
    }
}
