using Microsoft.EntityFrameworkCore;
using UnionRailway;

namespace UnionRailway.Demo.Services;

/// <summary>
/// Simulates a legacy service that throws exceptions — used in Step 13 (UnionWrapper).
/// </summary>
public sealed class LegacyInventoryService
{
    public Task<Product> GetProductAsync(int id)
    {
        return id switch
        {
            999 => throw new KeyNotFoundException($"Product {id} not found"),
            401 => throw new UnauthorizedAccessException("Access denied"),
            _   => Task.FromResult(new Product { Id = id, Name = "Legacy Product", Sku = $"LEG-{id:000}", Price = 9.99m, Stock = 5 })
        };
    }
}
