using Microsoft.EntityFrameworkCore;
using UnionRailway;
using UnionRailway.EntityFrameworkCore;

/// <summary>
/// Product catalog service integrated with UnionRailway.
///
/// Every method returns <see cref="Rail{T}"/> instead of throwing or returning
/// null — callers handle all outcomes explicitly and can still deconstruct the
/// result into value/error slots when desired.
/// </summary>
sealed class ProductService(AppDbContext db)
{
    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Returns the product with the given ID, or NotFound.</summary>
    public ValueTask<Rail<Product>> GetByIdAsync(
        int id, CancellationToken ct = default)
        => db.Products.FirstOrDefaultAsUnionAsync("Product", p => p.Id == id, ct);

    /// <summary>Returns the product matching the given SKU, or NotFound.</summary>
    public ValueTask<Rail<Product>> GetBySkuAsync(
        string sku, CancellationToken ct = default)
        => db.Products.FirstOrDefaultAsUnionAsync("Product", p => p.Sku == sku, ct);

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new product after validating inputs and checking for SKU uniqueness.
    /// Returns Validation if inputs are invalid, Conflict if the SKU already exists.
    /// </summary>
    public async ValueTask<Rail<Product>> CreateAsync(
        string name, string sku, decimal price, int stock,
        CancellationToken ct = default)
    {
        // Input validation — caught before any DB round-trip
        var errs = new List<(string Field, string[] Messages)>();
        if (string.IsNullOrWhiteSpace(name))  errs.Add(("Name",  ["Name is required"]));
        if (string.IsNullOrWhiteSpace(sku))   errs.Add(("Sku",   ["SKU is required"]));
        if (price <= 0)                        errs.Add(("Price", [$"Price must be positive (got {price})"]));
        if (stock < 0)                         errs.Add(("Stock", ["Stock cannot be negative"]));

        if (errs.Count > 0)
            return Union.Fail<Product>(UnionError.CreateValidation(errs));

        // Guard against duplicate SKUs
        var duplicate = await db.Products.AnyAsync(p => p.Sku == sku, ct);
        if (duplicate)
            return Union.Fail<Product>(
                new UnionError.Conflict($"A product with SKU '{sku}' already exists"));

        var product = new Product { Name = name, Sku = sku, Price = price, StockQty = stock };
        db.Products.Add(product);

        var (_, saveErr) = await db.SaveChangesAsUnionAsync(ct);
        return saveErr is not null
            ? Union.Fail<Product>(saveErr.GetValueOrDefault())
            : Union.Ok(product);
    }

    /// <summary>
    /// Decrements stock by <paramref name="qty"/>.
    /// Returns NotFound if the product doesn't exist, Conflict if stock is insufficient.
    /// </summary>
    public async ValueTask<Rail<Unit>> DeductStockAsync(
        int productId, int qty, CancellationToken ct = default)
    {
        var (product, err) = await GetByIdAsync(productId, ct);
        if (err is not null) return Union.Fail<Unit>(err.GetValueOrDefault());

        if (product!.StockQty < qty)
            return Union.Fail<Unit>(new UnionError.Conflict(
                $"Insufficient stock for '{product.Name}': " +
                $"requested {qty}, available {product.StockQty}"));

        product.StockQty -= qty;

        var (_, saveErr) = await db.SaveChangesAsUnionAsync(ct);
        return saveErr is not null
            ? Union.Fail<Unit>(saveErr.GetValueOrDefault())
            : Union.Ok();
    }
}
