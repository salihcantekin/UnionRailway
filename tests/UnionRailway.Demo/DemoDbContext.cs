using Microsoft.EntityFrameworkCore;

namespace UnionRailway.Demo;

public sealed class DemoDbContext(DbContextOptions<DemoDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order>   Orders   => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();

        // Seed data
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop Pro",   Sku = "LAP-001", Price = 1299.99m, Stock = 10 },
            new Product { Id = 2, Name = "Wireless Mouse", Sku = "MOU-002", Price = 29.99m,  Stock = 50 },
            new Product { Id = 3, Name = "USB-C Hub",    Sku = "HUB-003", Price = 49.99m,  Stock = 0  }
        );
    }
}
