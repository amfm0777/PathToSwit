using TelecomOps.Core;

namespace TelecomOps.Worker;

public class TelemetryWorker(
    ILogger<TelemetryWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    IMetricsPublisher metricsPublisher,
    IConfiguration configuration) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(
        int.Parse(configuration["Worker:IntervalSeconds"] ?? "5"));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TelemetryWorker started with interval: {interval}s", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var nodeConfigRepository = scope.ServiceProvider.GetRequiredService<INodeConfigRepository>();
                    var activeNodes = await nodeConfigRepository.GetActiveNodesAsync();

                    if (!activeNodes.Any())
                    {
                        logger.LogInformation("No active nodes found");
                        continue;
                    }

                    foreach (var node in activeNodes)
                    {
                        var telemetryEvent = GenerateTelemetryEvent(node);
                        await metricsPublisher.PublishAsync(telemetryEvent);
                        logger.LogInformation("Published telemetry for node {nodeId}: Latency={latency}ms, Throughput={throughput}Mbps",
                            node.Id, telemetryEvent.LatencyMs, telemetryEvent.ThroughputMbps);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in telemetry generation cycle");
            }
        }
    }

    private static TelemetryEvent GenerateTelemetryEvent(NodeConfig node)
    {
        var random = new Random();
        return new TelemetryEvent
        {
            NodeId = node.Id,
            Timestamp = DateTime.UtcNow,
            LatencyMs = random.NextDouble() * 100, // 0-100ms
            ThroughputMbps = random.NextDouble() * 1000, // 0-1000Mbps
            Status = node.Status,
            AdditionalData = $"Band:{node.FrequencyBand}"
        };
    }
}
