using Microsoft.EntityFrameworkCore;
using UnionRailway;
using UnionRailway.EntityFrameworkCore;

namespace UnionRailway.Demo.Services;

/// <summary>
/// Core product service — all methods return Rail<T>, never throw.
/// </summary>
public sealed class ProductService(DemoDbContext db)
{
    public async ValueTask<Rail<List<Product>>> GetAllAsync(CancellationToken ct = default)
        => await db.Products.ToListAsync(ct);

    public async ValueTask<Rail<Product>> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.Products.FirstOrDefaultAsUnionAsync("Product", p => p.Id == id, ct);

    public async ValueTask<Rail<Product>> GetBySkuAsync(string sku, CancellationToken ct = default)
        => await db.Products.FirstOrDefaultAsUnionAsync("Product", p => p.Sku == sku, ct);

    public async ValueTask<Rail<Product>> CreateAsync(CreateProductRequest req, CancellationToken ct = default)
    {
        // Validation
        var errors = new List<(string, string[])>();
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            errors.Add(("Name",  ["Name is required"]));
        }

        if (string.IsNullOrWhiteSpace(req.Sku))
        {
            errors.Add(("Sku",   ["SKU is required"]));
        }

        if (req.Price <= 0)
        {
            errors.Add(("Price", ["Price must be greater than 0"]));
        }

        if (errors.Count > 0)
        {
            return Union.Fail<Product>(UnionError.CreateValidation(errors));
        }

        // Conflict check
        var existing = await db.Products.FirstOrDefaultAsync(p => p.Sku == req.Sku, ct);
        if (existing is not null)
        {
            return Union.Fail<Product>(new UnionError.Conflict($"SKU '{req.Sku}' already exists"));
        }

        var product = new Product { Name = req.Name, Sku = req.Sku, Price = req.Price, Stock = req.Stock };
        db.Products.Add(product);
        var saved = await db.SaveChangesAsUnionAsync(ct);
        if (!saved.IsSuccess(out _, out var err))
        {
            return Union.Fail<Product>(err.GetValueOrDefault());
        }

        return product;
    }

    public async ValueTask<Rail<Unit>> DeductStockAsync(int productId, int qty, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([productId], ct);
        if (product is null)
        {
            return Union.Fail<Unit>(new UnionError.NotFound("Product"));
        }

        if (product.Stock < qty)
        {
            return Union.Fail<Unit>(new UnionError.Conflict($"Only {product.Stock} units in stock"));
        }

        product.Stock -= qty;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
