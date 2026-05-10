namespace TelecomOps.Core;

public interface IMetricsPublisher
{
    Task PublishAsync(TelemetryEvent telemetryEvent);
}