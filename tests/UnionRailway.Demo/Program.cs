using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using UnionRailway.AspNetCore;
using UnionRailway.Demo;
using UnionRailway.Demo.Endpoints;
using UnionRailway.Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "UnionRailway Demo API",
        Version     = "v1",
        Description =
            "A step-by-step showcase of all UnionRailway features. " +
            "Open each tag in order — each one introduces a new concept building on the previous."
    });
    c.TagActionsBy(api => api.GroupName is not null ? [api.GroupName] : [api.HttpMethod ?? "Other"]);
    c.DocInclusionPredicate((_, _) => true);
    c.OrderActionsBy(a => a.GroupName);
});

// EF Core — in-memory DB, no external dependencies
builder.Services.AddDbContext<DemoDbContext>(opt =>
    opt.UseInMemoryDatabase("UnionRailwayDemo"));

// Application services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<LegacyInventoryService>();

// HttpClient for Step 12 — uses the fake handler, no real network calls
builder.Services.AddHttpClient<ExternalCatalogClient>(c =>
    c.BaseAddress = new Uri("https://external-catalog.example.com"))
    .ConfigurePrimaryHttpMessageHandler(() => new FakeExternalHandler());

// ── UnionRailway — one-line DI setup (Step 09 global configureProblem) ───────
builder.Services.AddRailway(options =>
{
    // Global traceId injected into EVERY ProblemDetails response
    options.ConfigureProblem = pd => pd.Extensions["traceId"] = Activity.Current?.Id ?? "no-active-trace";
});

var app = builder.Build();

// ── Seed the in-memory DB ─────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
    db.Database.EnsureCreated();
}

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UnionRailway Demo v1");
    c.RoutePrefix       = string.Empty;   // Swagger UI at root /
    c.DocumentTitle     = "UnionRailway Demo";
    c.DefaultModelsExpandDepth(-1);       // collapse schemas by default
});

// Catches unhandled exceptions → RFC 7807 (Step 13 context)
app.UseRailwayExceptionHandler();

// ── Demo endpoint groups ─────────────────────────────────────────────────────
// Each MapGroup is tagged so Swagger shows them as collapsible sections in order.

var demo = app.MapGroup("/demo");

demo.MapStep01();
demo.MapStep02();
demo.MapStep03();
demo.MapStep04();
demo.MapStep05();
demo.MapStep06();
demo.MapStep07();
demo.MapStep08();
demo.MapStep09();
demo.MapStep10();
demo.MapStep11();
demo.MapStep12();
demo.MapStep13();
demo.MapStep14();
demo.MapStep15();

app.Run();

public partial class Program { }

