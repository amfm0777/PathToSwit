using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TelecomOps.Core;
using TelecomOps.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<INodeConfigRepository, NodeConfigRepository>();
builder.Services.AddScoped<INodeConfigService, NodeConfigService>();

builder.Services.AddSingleton<IMetricsPublisher>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new InfluxDbMetricsPublisher(
        config["InfluxDb:Url"]!,
        config["InfluxDb:Token"]!,
        config["InfluxDb:Org"]!,
        config["InfluxDb:Bucket"]!
    );
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database schema exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if ((await db.Database.GetPendingMigrationsAsync()).Any())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapGet("/", () => "TelecomOps API");

app.MapGet("/nodes", async (INodeConfigRepository repo) =>
{
    var nodes = await repo.GetAllAsync();
    return Results.Ok(nodes);
});

app.MapGet("/nodes/active", async (INodeConfigRepository repo) =>
{
    var nodes = await repo.GetActiveNodesAsync();
    return Results.Ok(nodes);
});

app.MapGet("/nodes/{id:guid}", async (INodeConfigRepository repo, Guid id) =>
{
    var node = await repo.GetByIdAsync(id);
    return node is null ? Results.NotFound() : Results.Ok(node);
});

app.MapPost("/nodes", async (INodeConfigService service, string name, FrequencyBand frequencyBand) =>
{
    var node = await service.CreateNodeConfigAsync(name, frequencyBand);
    return Results.Created($"/nodes/{node.Id}", node);
});

app.MapPut("/nodes/{id:guid}/status", async (INodeConfigService service, Guid id, [Microsoft.AspNetCore.Mvc.FromQuery] NodeStatus status) =>
{
    try
    {
        await service.UpdateNodeStatusAsync(id, status);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapPost("/nodes/{id:guid}/telemetry", async (IMetricsPublisher publisher, Guid id, TelemetryRequest req) =>
{
    var evt = new TelemetryEvent
    {
        NodeId = id,
        Timestamp = DateTime.UtcNow,
        LatencyMs = req.LatencyMs,
        ThroughputMbps = req.ThroughputMbps,
        Status = NodeStatus.Active,
        AdditionalData = req.AdditionalData
    };
    await publisher.PublishAsync(evt);
    return Results.NoContent();
});

app.MapDelete("/nodes/{id:guid}", async (INodeConfigRepository repo, Guid id) =>
{
    var node = await repo.GetByIdAsync(id);
    if (node is null) return Results.NotFound();
    await repo.DeleteAsync(id);
    return Results.NoContent();
});

app.Run();

record TelemetryRequest(double LatencyMs, double ThroughputMbps, string? AdditionalData = null);
