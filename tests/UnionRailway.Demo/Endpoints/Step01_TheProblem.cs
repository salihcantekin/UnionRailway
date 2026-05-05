using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnionRailway.Demo.Services;

namespace UnionRailway.Demo.Endpoints;

/// <summary>
/// 📖 STEP 01 — What is the Problem?
///
/// We are writing an e-commerce API. Even simple operations like fetching or creating
/// a product are flooded with try/catch chains. Every endpoint returns a different error shape:
/// one returns { field, error }, another returns just a 404, another a Problem(). It's inconsistent!
///
/// ❓ Question: How do you know what errors an endpoint might throw?
///          Who tells the caller about the contract? Documentation? Luck?
///
/// In the next step, we will bring order to this chaos using Rail<T>.
/// </summary>
public static class Step01_TheProblem
{
    public static RouteGroupBuilder MapStep01(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/step01").WithTags("01 - The Problem (Before UnionRailway)");




        // ── Classic: exceptions for control flow ─────────────────────────────
        // The caller has NO idea this can throw. The try/catch is scattered.
        // The error contract is implicit — it lives only in documentation (if at all).
        group.MapGet("/products/{id:int}", async (int id, DemoDbContext db) =>
        {
            // 💡 Ask: "What errors can this return? How would the caller know?"
            try
            {
                var product = await db.Products.FindAsync(id)
                    ?? throw new KeyNotFoundException($"Product {id} not found");

                return Results.Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithSummary("GET product — classic exception-based approach")
        .WithDescription(
            "⚠️ BEFORE: try/catch everywhere, no type safety, caller doesn't know what errors to expect. " +
            "Try id=1 (found), id=999 (throws KeyNotFoundException → 404), id=0 (throws → 404).");









        // ── Classic: nullable returns ─────────────────────────────────────────
        group.MapGet("/products/by-sku/{sku}", async (string sku, DemoDbContext db) =>
        {
            // 💡 Ask: "What does null mean here? Not found? Deleted? No permission?"
            var product = await db.Products.FirstOrDefaultAsync(p => p.Sku == sku);
            if (product is null)
            {
                return Results.NotFound();                 // 🤷 lost all context
            }

            return Results.Ok(product);
        })
        .WithSummary("GET product by SKU — nullable return problem")
        .WithDescription(
            "⚠️ BEFORE: null means 'not found' but could mean anything. " +
            "Try sku=LAP-001 (found), sku=UNKNOWN (null → 404 with no context).");








        // ── Classic: inconsistent validation errors ───────────────────────────
        group.MapPost("/products", async ([FromBody] CreateProductRequest req, DemoDbContext db) =>
        {
            // 💡 Ask: "Validation errors look different from not-found errors — inconsistent!"
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new { field = "Name", error = "Required" });
            }

            if (req.Price <= 0)
            {
                return Results.BadRequest(new { field = "Price", error = "Must be > 0" });
            }

            // Check duplicate — yet another return shape
            var exists = await db.Products.AnyAsync(p => p.Sku == req.Sku);
            if (exists)
            {
                return Results.Conflict(new { error = $"SKU '{req.Sku}' already taken" });
            }

            var product = new Product { Name = req.Name, Sku = req.Sku, Price = req.Price, Stock = req.Stock };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Results.Created($"/products/{product.Id}", product);
        })
        .WithSummary("POST product — inconsistent error shapes")
        .WithDescription(
            "⚠️ BEFORE: validation errors, conflict errors, success — all return different shapes. " +
            "Try empty name, negative price, duplicate SKU=LAP-001, or a valid new product.");

        return app;
    }
}
