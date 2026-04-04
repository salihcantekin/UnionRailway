using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UnionRailway.EntityFrameworkCore;

namespace UnionRailway.Tests.EntityFrameworkCore;

public record TestEntity
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}

public class QueryableExtensionsTests : IDisposable
{
    private readonly TestDbContext context;

    public QueryableExtensionsTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        this.context = new TestDbContext(options);
        SeedData();
    }

    private void SeedData()
    {
        this.context.TestEntities.AddRange(
        [
            new TestEntity { Id = 1, Name = "Alice", Email = "alice@example.com" },
            new TestEntity { Id = 2, Name = "Bob", Email = "bob@example.com" },
            new TestEntity { Id = 3, Name = "Charlie", Email = "charlie@example.com" }
        ]);
        this.context.SaveChanges();
    }

    [Fact]
    public async Task FirstOrDefaultAsUnionAsync_ShouldReturnOk_WhenEntityExists()
    {
        var result = await this.context.TestEntities
            .Where(e => e.Name == "Alice")
            .FirstOrDefaultAsUnionAsync();

        result.Should().BeOfType<Result<TestEntity>.Ok>();
        var ok = (Result<TestEntity>.Ok)result;
        ok.Data.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task FirstOrDefaultAsUnionAsync_ShouldReturnNotFound_WhenEntityDoesNotExist()
    {
        var result = await this.context.TestEntities
            .Where(e => e.Name == "Nobody")
            .FirstOrDefaultAsUnionAsync();

        result.Should().BeOfType<Result<TestEntity>.Error>();
        var error = (Result<TestEntity>.Error)result;
        error.Err.Should().BeOfType<UnionError.NotFound>();
    }

    [Fact]
    public async Task SingleOrDefaultAsUnionAsync_ShouldReturnOk_WhenSingleEntityExists()
    {
        var result = await this.context.TestEntities
            .Where(e => e.Id == 2)
            .SingleOrDefaultAsUnionAsync();

        result.Should().BeOfType<Result<TestEntity>.Ok>();
        var ok = (Result<TestEntity>.Ok)result;
        ok.Data.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task SingleOrDefaultAsUnionAsync_ShouldReturnNotFound_WhenNoEntityExists()
    {
        var result = await this.context.TestEntities
            .Where(e => e.Id == 999)
            .SingleOrDefaultAsUnionAsync();

        result.Should().BeOfType<Result<TestEntity>.Error>();
        var error = (Result<TestEntity>.Error)result;
        error.Err.Should().BeOfType<UnionError.NotFound>();
    }

    [Fact]
    public async Task ToListAsUnionAsync_ShouldReturnOk_WithAllEntities()
    {
        var result = await this.context.TestEntities
            .ToListAsUnionAsync();

        result.Should().BeOfType<Result<List<TestEntity>>.Ok>();
        var ok = (Result<List<TestEntity>>.Ok)result;
        ok.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task ToListAsUnionAsync_ShouldReturnOk_WithEmptyList_WhenNoMatch()
    {
        var result = await this.context.TestEntities
            .Where(e => e.Name == "Nobody")
            .ToListAsUnionAsync();

        result.Should().BeOfType<Result<List<TestEntity>>.Ok>();
        var ok = (Result<List<TestEntity>>.Ok)result;
        ok.Data.Should().BeEmpty();
    }

    public void Dispose()
    {
        this.context.Dispose();
        GC.SuppressFinalize(this);
    }
}
