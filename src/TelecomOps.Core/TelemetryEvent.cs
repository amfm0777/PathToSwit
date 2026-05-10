namespace TelecomOps.Core;

public class TelemetryEvent
{
    public Guid NodeId { get; set; }
    public DateTime Timestamp { get; set; }
    public double LatencyMs { get; set; }
    public double ThroughputMbps { get; set; }
    public NodeStatus Status { get; set; }
    public string? AdditionalData { get; set; }
}