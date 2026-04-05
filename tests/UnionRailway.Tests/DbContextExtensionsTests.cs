using Microsoft.EntityFrameworkCore;
using UnionRailway;
using UnionRailway.EntityFrameworkCore;

namespace UnionRailway.Tests;

// ── Minimal in-memory EF setup ───────────────────────────────────────────────────

public sealed class BlogPost
{
    public int    Id    { get; set; }
    public string Title { get; set; } = "";
}

public sealed class BlogContext(DbContextOptions<BlogContext> options)
    : DbContext(options)
{
    public DbSet<BlogPost> Posts => Set<BlogPost>();
}

// ── Tests ────────────────────────────────────────────────────────────────────────

public sealed class DbContextExtensionsTests : IDisposable
{
    private readonly BlogContext context;

    public DbContextExtensionsTests()
    {
        var options = new DbContextOptionsBuilder<BlogContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        context = new BlogContext(options);
    }

    public void Dispose() => context.Dispose();

    // ── FirstOrDefaultAsUnionAsync ────────────────────────────────────────────

    [Fact]
    public async Task FirstOrDefaultAsUnionAsync_EntityExists_ReturnsOk()
    {
        context.Posts.Add(new BlogPost { Id = 1, Title = "Hello" });
        await context.SaveChangesAsync();

        var result = await context.Posts
            .FirstOrDefaultAsUnionAsync("BlogPost", p => p.Id == 1);

        Assert.Null(result.Error);
        Assert.Equal("Hello", result.Unwrap().Title);
    }

    [Fact]
    public async Task FirstOrDefaultAsUnionAsync_EntityMissing_ReturnsNotFound()
    {
        var result = await context.Posts
            .FirstOrDefaultAsUnionAsync("BlogPost", p => p.Id == 999);

        Assert.NotNull(result.Error);
        var nf = Assert.IsType<UnionError.NotFound>(result.Error);
        Assert.Equal("BlogPost", nf.Resource);
    }

    [Fact]
    public async Task FirstOrDefaultAsUnionAsync_NoPredicate_ReturnsFirstRow()
    {
        context.Posts.AddRange(
            new BlogPost { Id = 1, Title = "First" },
            new BlogPost { Id = 2, Title = "Second" });
        await context.SaveChangesAsync();

        var result = await context.Posts.FirstOrDefaultAsUnionAsync("BlogPost");

        Assert.Null(result.Error);
    }

    // ── SaveChangesAsUnionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsUnionAsync_ValidEntity_ReturnsAffectedCount()
    {
        context.Posts.Add(new BlogPost { Id = 10, Title = "New Post" });

        var result = await context.SaveChangesAsUnionAsync();

        Assert.Null(result.Error);
        Assert.Equal(1, result.Unwrap());
    }

    [Fact]
    public async Task SaveChangesAsUnionAsync_NoChanges_ReturnsZero()
    {
        var result = await context.SaveChangesAsUnionAsync();

        Assert.Null(result.Error);
        Assert.Equal(0, result.Value);
    }
}
