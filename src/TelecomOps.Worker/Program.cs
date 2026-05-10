using Microsoft.EntityFrameworkCore;
using TelecomOps.Core;
using TelecomOps.Infrastructure;
using TelecomOps.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddEnvironmentVariables();
System.Console.WriteLine("TelecomOps.Worker*****:: Configuration loaded from environment variables:");
foreach (var c in builder.Configuration.AsEnumerable())
{
    System.Console.WriteLine($"TelecomOps.Worker*****::  {c.Key}: {c.Value}");
}

// Add services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<INodeConfigRepository, NodeConfigRepository>();

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

builder.Services.AddHostedService<TelemetryWorker>();

var host = builder.Build();

// Ensure database schema exists
using (var scope = host.Services.CreateScope())
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

host.Run();
