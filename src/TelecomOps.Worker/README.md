# TelecomOps.Worker — Background Service Layer

## Purpose

Long-running background service that generates simulated telemetry metrics for active nodes and publishes them to InfluxDB.

## Key Components

### TelemetryWorker.cs

Extends `BackgroundService` (IHostedService).

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
```

- Runs on startup and continues until application shutdown
- Uses `PeriodicTimer` to execute every N seconds (configurable, default 5s)
- For each iteration:
  1. Creates a service scope (required for accessing Scoped repositories)
  2. Gets all active nodes from database
  3. For each active node, generates a simulated `TelemetryEvent`
  4. Publishes the event to InfluxDB
  5. Logs the operation

### Configuration

```csharp
Worker__IntervalSeconds = 5  // Execute every 5 seconds
```

### Dependency Injection Pattern

```csharp
public TelemetryWorker(
    ILogger<TelemetryWorker> logger,           // Logging
    IServiceScopeFactory serviceScopeFactory,  // Required for Scoped services
    IMetricsPublisher metricsPublisher,        // Singleton: InfluxDB publisher
    IConfiguration configuration)              // Configuration
```

**Important**: Cannot directly inject `INodeConfigRepository` (Scoped) into a Singleton service. Solution: Use `IServiceScopeFactory.CreateScope()` to get a Scoped instance inside the loop.

### Telemetry Event Generation

```csharp
private static TelemetryEvent GenerateTelemetryEvent(NodeConfig node)
{
    return new TelemetryEvent
    {
        NodeId = node.Id,
        Timestamp = DateTime.UtcNow,
        LatencyMs = random.NextDouble() * 100,      // Random 0-100ms
        ThroughputMbps = random.NextDouble() * 1000, // Random 0-1000Mbps
        Status = node.Status,
        AdditionalData = $"Band:{node.FrequencyBand}"
    };
}
```

Generates realistic-looking metrics:
- Latency varies between 0-100ms
- Throughput varies between 0-1000 Mbps
- Includes node state and frequency band info

## Configuration

```csharp
// appsettings.json
{
  "Worker": {
    "IntervalSeconds": 5
  },
  "ConnectionStrings": {
    "Default": "..."
  },
  "InfluxDb": {
    "Url": "http://localhost:8086",
    "Token": "...",
    "Org": "telecomops",
    "Bucket": "metrics"
  }
}
```

## Dependency Injection

- `ILogger<TelemetryWorker>`: For console/file logging
- `IServiceScopeFactory`: To create scopes for Scoped services
- `IMetricsPublisher`: Singleton for InfluxDB
- `IConfiguration`: To read Worker__IntervalSeconds

Database context is obtained inside the scope:
```csharp
using (var scope = serviceScopeFactory.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<INodeConfigRepository>();
    // Use repo here
} // Disposed after this block
```

## Running

```bash
# Local development
dotnet run --project src/TelecomOps.Worker/

# In Docker
docker compose up worker
```

## Logs

Look for messages like:
```
Published telemetry for node 12345: Latency=45.2ms, Throughput=523.8Mbps
```

## Why this layer exists

- **Decoupled from HTTP**: Runs independently of the API.
- **Scalability**: Can run on separate containers/servers.
- **Data generation**: Simulates realistic 5G network behavior.
- **Continuous monitoring**: Provides ongoing telemetry stream to observability stack.
