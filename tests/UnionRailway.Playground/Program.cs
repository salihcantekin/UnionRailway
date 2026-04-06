// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  UnionRailway.Playground — E-Commerce Back-End Demo                      ║
// ║                                                                          ║
// ║  Simulates a small online-store back-end that has been migrated to use   ║
// ║  UnionRailway.  EF Core powers the product/order database; an external   ║
// ║  payment gateway is called via HttpClient.  Neither layer throws or      ║
// ║  returns null — every outcome is a typed union.                          ║
// ║                                                                          ║
// ║  Run:   dotnet run --project tests/UnionRailway.Playground               ║
// ║  Debug: open this file in VS Code and press F5.                          ║
// ╚══════════════════════════════════════════════════════════════════════════╝

using UnionRailway;

// ── Bootstrap — wire services (mirrors a minimal DI container) ────────────────

var db = await TestDatabase.CreateWithSeedDataAsync();

using var paymentHandler = new FakePaymentHandler();
using var paymentHttp    = new System.Net.Http.HttpClient(paymentHandler)
    { BaseAddress = new Uri("https://payments.example.com") };

var productSvc = new ProductService(db);
var paymentSvc = new PaymentGatewayClient(paymentHttp);
var orderSvc   = new OrderService(db, productSvc, paymentSvc);

// ── Run scenarios ─────────────────────────────────────────────────────────────

Banner("UnionRailway Playground — E-Commerce Demo");

await BrowseCatalog();
await GetNonExistentProduct();
await PlaceSuccessfulOrder();
await OrderValidationFailure();
await OrderWithInsufficientStock();
await OrderWithDeclinedCard();
await OrderWithBlockedCard();
await AddNewProduct();
await CreateProductWithBadInput();
await DuplicateSkuConflict();
await LookupBySku();

Banner("All scenarios completed");

// ═════════════════════════════════════════════════════════════════════════════
//  SCENARIOS
//  Each function models a typical controller action or application use case.
//  The service layer returns unions; here we pattern-match on the result to
//  decide what to output — exactly what a real controller would do to choose
//  an HTTP status code and response body.
// =============================================================================

async Task BrowseCatalog()
{
    Heading("Browse product catalog  [GET /products]");

    foreach (var id in new[] { 1, 2, 3 })
    {
        var (product, err) = await productSvc.GetByIdAsync(id);
        if (err is null)
            OK($"[{product!.Id}] {product}");
        else
            Fail($"Unexpected error for #{id}: {err}");
    }
}

async Task GetNonExistentProduct()
{
    Heading("Lookup a product that doesn't exist  [GET /products/999]");

    var (_, err) = await productSvc.GetByIdAsync(999);

    switch (err?.Value)
    {
        case UnionError.NotFound nf:
            Fail($"404 — resource '{nf.Resource}' does not exist");
            break;
        default:
            Fail($"Unexpected error type: {err}");
            break;
    }
}

async Task PlaceSuccessfulOrder()
{
    Heading("Place a successful order  [POST /orders — valid card, stock available]");

    var req = new CreateOrderRequest(CustomerId: 42, ProductId: 1, Quantity: 3);
    var (order, err) = await orderSvc.PlaceOrderAsync(req, cardToken: "card-valid");

    if (err is null)
    {
        OK($"Order #{order!.Id} confirmed");
        Info($"  Customer  : #{order.CustomerId}");
        Info($"  Product   : #{order.ProductId}  ×  {order.Quantity} units");
        Info($"  Total     : ${order.TotalPrice:F2}");
        Info($"  Status    : {order.Status}");

        // Show that stock was deducted
        var (p, _) = await productSvc.GetByIdAsync(req.ProductId);
        Info($"  Stock now : {p!.StockQty} remaining");
    }
    else
    {
        Fail($"Unexpected failure: {err}");
    }
}

async Task OrderValidationFailure()
{
    Heading("Order with invalid input  [POST /orders — qty=0, no card token]");

    var req = new CreateOrderRequest(CustomerId: 42, ProductId: 1, Quantity: 0);
    var (_, err) = await orderSvc.PlaceOrderAsync(req, cardToken: "");

    switch (err?.Value)
    {
        case UnionError.Validation v:
            Fail("400 — validation errors (no DB round-trip made):");
            foreach (var (field, messages) in v.Fields)
                foreach (var msg in messages)
                    Info($"       [{field}]  {msg}");
            break;
        default:
            Fail($"Expected Validation, got: {err?.GetType().Name}");
            break;
    }
}

async Task OrderWithInsufficientStock()
{
    Heading("Order quantity exceeds available stock  [POST /orders — qty=50, stock=10]");

    // Product #2 has only 10 units in stock
    var req = new CreateOrderRequest(CustomerId: 42, ProductId: 2, Quantity: 50);
    var (_, err) = await orderSvc.PlaceOrderAsync(req, cardToken: "card-valid");

    switch (err?.Value)
    {
        case UnionError.Conflict c:
            Fail($"409 — {c.Reason}");
            break;
        default:
            Fail($"Expected Conflict, got: {err}");
            break;
    }
}

async Task OrderWithDeclinedCard()
{
    Heading("Order with a declined card  [POST /orders — card-declined]");

    // Capture stock level before the attempt
    var (before, _) = await productSvc.GetByIdAsync(1);
    var stockBefore = before!.StockQty;

    var req = new CreateOrderRequest(CustomerId: 42, ProductId: 1, Quantity: 1);
    var (_, err) = await orderSvc.PlaceOrderAsync(req, cardToken: "card-declined");

    switch (err?.Value)
    {
        case UnionError.Conflict c:
            Fail($"409 — payment declined: {c.Reason}");
            break;
        default:
            Fail($"Expected Conflict, got: {err}");
            break;
    }

    // Verify the reserved stock was rolled back after the payment failure
    var (after, _) = await productSvc.GetByIdAsync(1);
    var stockAfter = after!.StockQty;

    if (stockBefore == stockAfter)
        OK($"Stock rolled back correctly — still {stockAfter} units (no leak)");
    else
        Fail($"Stock leak! before={stockBefore}, after={stockAfter}");
}

async Task OrderWithBlockedCard()
{
    Heading("Order with a blocked / stolen card  [POST /orders — card-stolen]");

    var req = new CreateOrderRequest(CustomerId: 42, ProductId: 1, Quantity: 1);
    var (_, err) = await orderSvc.PlaceOrderAsync(req, cardToken: "card-stolen");

    switch (err?.Value)
    {
        case UnionError.Forbidden f:
            Fail($"403 — {f.Reason}");
            break;
        default:
            Fail($"Expected Forbidden, got: {err}");
            break;
    }
}

async Task AddNewProduct()
{
    Heading("Admin: add a new product  [POST /admin/products]");

    var (product, err) = await productSvc.CreateAsync(
        name: "Turbo Widget", sku: "TWG-099", price: 79.99m, stock: 25);

    if (err is null)
        OK($"201 — created: [{product!.Id}] {product}");
    else
        Fail($"Failed: {err}");
}

async Task CreateProductWithBadInput()
{
    Heading("Admin: create product with invalid data  [blank name, price=-5]");

    var (_, err) = await productSvc.CreateAsync(
        name: "", sku: "X", price: -5m, stock: 10);

    switch (err?.Value)
    {
        case UnionError.Validation v:
            Fail("400 — validation errors:");
            foreach (var (field, messages) in v.Fields)
                foreach (var msg in messages)
                    Info($"       [{field}]  {msg}");
            break;
        default:
            Fail($"Expected Validation, got: {err}");
            break;
    }
}

async Task DuplicateSkuConflict()
{
    Heading("Admin: create product with an already-used SKU  [SKU conflict]");

    var (_, err) = await productSvc.CreateAsync(
        name: "Another Gadget", sku: "BGT-002", price: 12.99m, stock: 5);

    switch (err?.Value)
    {
        case UnionError.Conflict c:
            Fail($"409 — {c.Reason}");
            break;
        default:
            Fail($"Expected Conflict, got: {err}");
            break;
    }
}

async Task LookupBySku()
{
    Heading("Lookup products by SKU  [GET /products?sku=...]");

    foreach (var sku in new[] { "WGT-001", "PRM-003", "UNKNOWN-99" })
    {
        var (product, err) = await productSvc.GetBySkuAsync(sku);

        var line = err?.Value switch
        {
            null                => $"✓  {sku,-14}  →  {product}",
            UnionError.NotFound => $"✗  {sku,-14}  →  not found",
            _                   => $"✗  {sku,-14}  →  unexpected error: {err}"
        };

        Console.WriteLine($"   {line}");
    }
}

// ── Console helpers ───────────────────────────────────────────────────────────

static void Banner(string title)
{
    var bar = new string('═', title.Length + 4);
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(bar);
    Console.WriteLine($"  {title}  ");
    Console.WriteLine(bar);
    Console.ResetColor();
}

static void Heading(string text)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n▶  {text}");
    Console.ResetColor();
}

static void OK(string msg)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"   ✓  {msg}");
    Console.ResetColor();
}

static void Fail(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"   ✗  {msg}");
    Console.ResetColor();
}

static void Info(string msg) => Console.WriteLine(msg);
