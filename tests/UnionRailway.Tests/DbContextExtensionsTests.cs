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
    private static TError AssertError<T, TError>(Rail<T> result)
        where TError : class
    {
        Assert.True(result.TryGetError(out UnionError? error));
        return Assert.IsType<TError>(error.GetValueOrDefault().Value);
    }

    private readonly BlogContext context;

    public DbContextExtensionsTests()
    {
        DbContextOptions<BlogContext> options = new DbContextOptionsBuilder<BlogContext>()
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

        Rail<BlogPost> result = await context.Posts
            .FirstOrDefaultAsUnionAsync("BlogPost", p => p.Id == 1);

        Assert.False(result.TryGetError(out _));
        Assert.Equal("Hello", result.Unwrap().Title);
    }

    [Fact]
    public async Task FirstOrDefaultAsUnionAsync_EntityMissing_ReturnsNotFound()
    {
        Rail<BlogPost> result = await context.Posts
            .FirstOrDefaultAsUnionAsync("BlogPost", p => p.Id == 999);

        UnionError.NotFound nf = AssertError<BlogPost, UnionError.NotFound>(result);
        Assert.Equal("BlogPost", nf.Resource);
    }

    [Fact]
    public async Task FirstOrDefaultAsUnionAsync_NoPredicate_ReturnsFirstRow()
    {
        context.Posts.AddRange(
            new BlogPost { Id = 1, Title = "First" },
            new BlogPost { Id = 2, Title = "Second" });
        await context.SaveChangesAsync();

        Rail<BlogPost> result = await context.Posts.FirstOrDefaultAsUnionAsync("BlogPost");

        Assert.Equal("First", result.Unwrap().Title);
    }

    // ── SaveChangesAsUnionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsUnionAsync_ValidEntity_ReturnsAffectedCount()
    {
        context.Posts.Add(new BlogPost { Id = 10, Title = "New Post" });

        Rail<int> result = await context.SaveChangesAsUnionAsync();

        Assert.Equal(1, result.Unwrap());
    }

    [Fact]
    public async Task SaveChangesAsUnionAsync_NoChanges_ReturnsZero()
    {
        Rail<int> result = await context.SaveChangesAsUnionAsync();

        Assert.Equal(0, result.Unwrap());
    }

    [Fact]
    public async Task ToListAsUnionAsync_ReturnsMaterializedList()
    {
        context.Posts.AddRange(
            new BlogPost { Id = 1, Title = "First" },
            new BlogPost { Id = 2, Title = "Second" });
        await context.SaveChangesAsync();

        Rail<List<BlogPost>> result = await context.Posts.OrderBy(p => p.Id).ToListAsUnionAsync();

        Assert.Collection(result.Unwrap(),
            p => Assert.Equal("First", p.Title),
            p => Assert.Equal("Second", p.Title));
    }
}
