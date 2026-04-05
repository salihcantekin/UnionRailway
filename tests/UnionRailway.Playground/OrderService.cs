using UnionRailway;
using UnionRailway.EntityFrameworkCore;

/// <summary>
/// Application-layer service that orchestrates the full order placement workflow:
/// input validation → product lookup → stock reservation → payment → persistence.
///
/// Each step returns a union.  Because errors short-circuit at the first
/// <c>if (err is not null) return</c>, the happy path reads top-to-bottom
/// without nesting or try/catch blocks.
/// </summary>
sealed class OrderService(AppDbContext db, ProductService products, PaymentGatewayClient payments)
{
    public async ValueTask<(Order, UnionError?)> PlaceOrderAsync(
        CreateOrderRequest req, string cardToken, CancellationToken ct = default)
    {
        // ── Step 1: validate the request fields ───────────────────────────────
        var errs = new List<(string, string[])>();
        if (req.Quantity <= 0)
            errs.Add(("Quantity",  ["Quantity must be at least 1"]));
        if (req.Quantity > 100)
            errs.Add(("Quantity",  ["Cannot order more than 100 items at once"]));
        if (string.IsNullOrWhiteSpace(cardToken))
            errs.Add(("CardToken", ["Payment card token is required"]));

        if (errs.Count > 0)
            return Union.Fail<Order>(UnionError.CreateValidation(errs));

        // ── Step 2: look up the product ───────────────────────────────────────
        // Propagates NotFound automatically if the product doesn't exist.
        var (product, productErr) = await products.GetByIdAsync(req.ProductId, ct);
        if (productErr is not null) return Union.Fail<Order>(productErr);

        // ── Step 3: reserve stock ─────────────────────────────────────────────
        // Propagates Conflict if the requested quantity exceeds available stock.
        var (_, stockErr) = await products.DeductStockAsync(req.ProductId, req.Quantity, ct);
        if (stockErr is not null) return Union.Fail<Order>(stockErr);

        var total = product!.Price * req.Quantity;

        // ── Step 4: charge the card ───────────────────────────────────────────
        // Propagates Conflict (declined), Forbidden (blocked), etc. directly
        // from the payment gateway — no manual status-code inspection needed.
        var (payment, paymentErr) = await payments.ChargeAsync(
            new PaymentRequest(0, total, cardToken), ct);

        if (paymentErr is not null)
        {
            // Payment failed — roll back the reserved stock so it becomes
            // available again before propagating the payment error upward.
            product.StockQty += req.Quantity;
            await db.SaveChangesAsync(ct);
            return Union.Fail<Order>(paymentErr);
        }

        // ── Step 5: persist the confirmed order ───────────────────────────────
        var order = new Order
        {
            CustomerId = req.CustomerId,
            ProductId  = req.ProductId,
            Quantity   = req.Quantity,
            TotalPrice = total,
            Status     = $"Confirmed (txn: {payment!.TransactionId})",
            CreatedAt  = DateTime.UtcNow
        };

        db.Orders.Add(order);

        var (_, saveErr) = await db.SaveChangesAsUnionAsync(ct);
        return saveErr is not null
            ? Union.Fail<Order>(saveErr)
            : Union.Ok(order);
    }
}
