// ── EF Core entities ──────────────────────────────────────────────────────────

sealed class Product
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = "";
    public string  Sku      { get; set; } = "";
    public decimal Price    { get; set; }
    public int     StockQty { get; set; }

    public override string ToString() =>
        $"{Name} (SKU: {Sku}) @ ${Price:F2} | Stock: {StockQty}";
}

sealed class Order
{
    public int      Id         { get; set; }
    public int      CustomerId { get; set; }
    public int      ProductId  { get; set; }
    public int      Quantity   { get; set; }
    public decimal  TotalPrice { get; set; }
    public string   Status     { get; set; } = "Pending";
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}

// ── Request / response DTOs ───────────────────────────────────────────────────

record CreateOrderRequest(int CustomerId, int ProductId, int Quantity);
record PaymentRequest(int OrderId, decimal Amount, string CardToken);
record PaymentResponse(string TransactionId, string Status);
