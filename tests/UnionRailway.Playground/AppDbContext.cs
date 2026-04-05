using Microsoft.EntityFrameworkCore;

sealed class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order>   Orders   => Set<Order>();
}

/// <summary>
/// Factory that spins up a clean in-memory database seeded with sample data.
/// In a real application this would be replaced by a proper connection string.
/// </summary>
static class TestDatabase
{
    public static async Task<AppDbContext> CreateWithSeedDataAsync()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"shop-{Guid.NewGuid()}")
            .Options;

        var db = new AppDbContext(opts);

        db.Products.AddRange(
            new Product { Id = 1, Name = "Widget Pro",    Sku = "WGT-001", Price =  29.99m, StockQty = 100 },
            new Product { Id = 2, Name = "Budget Gadget", Sku = "BGT-002", Price =   9.99m, StockQty =  10 },
            new Product { Id = 3, Name = "Premium Kit",   Sku = "PRM-003", Price = 149.99m, StockQty =   5 }
        );

        await db.SaveChangesAsync();
        return db;
    }
}
