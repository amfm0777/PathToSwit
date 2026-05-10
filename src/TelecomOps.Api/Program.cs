using Microsoft.EntityFrameworkCore;
using TelecomOps.Core;
using TelecomOps.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

app.MapPost("/nodes", async (INodeConfigService service, string name, FrequencyBand frequencyBand) =>
{
    var node = await service.CreateNodeConfigAsync(name, frequencyBand);
    return Results.Created($"/nodes/{node.Id}", node);
});

app.Run();
