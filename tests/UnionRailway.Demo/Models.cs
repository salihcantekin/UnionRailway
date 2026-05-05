namespace UnionRailway.Demo;

public sealed class Product
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = "";
    public string  Sku      { get; set; } = "";
    public decimal Price    { get; set; }
    public int     Stock    { get; set; }
}

public sealed class Order
{
    public int      Id         { get; set; }
    public int      ProductId  { get; set; }
    public int      Quantity   { get; set; }
    public decimal  TotalPrice { get; set; }
    public string   Status     { get; set; } = "";
    public DateTime CreatedAt  { get; set; }
}

// ── Request / Response DTOs ──────────────────────────────────────────────────

public sealed record CreateProductRequest(string Name, string Sku, decimal Price, int Stock);
public sealed record PlaceOrderRequest(int ProductId, int Quantity, string CardToken);
public sealed record ExternalProductDto(int Id, string Name, decimal Price);
