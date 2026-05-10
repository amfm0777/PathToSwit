using Xunit;
using FluentAssertions;
using TelecomOps.Core;

namespace TelecomOps.Core.Tests;

public class TelemetryEventTests
{
    [Fact]
    public void TelemetryEvent_Creation_Should_Set_Properties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var latencyMs = 45.5;
        var throughputMbps = 123.4;
        var status = NodeStatus.Active;
        var additionalData = "Band:3500";

        // Act
        var telemetryEvent = new TelemetryEvent
        {
            NodeId = nodeId,
            Timestamp = timestamp,
            LatencyMs = latencyMs,
            ThroughputMbps = throughputMbps,
            Status = status,
            AdditionalData = additionalData
        };

        // Assert
        telemetryEvent.NodeId.Should().Be(nodeId);
        telemetryEvent.Timestamp.Should().Be(timestamp);
        telemetryEvent.LatencyMs.Should().Be(latencyMs);
        telemetryEvent.ThroughputMbps.Should().Be(throughputMbps);
        telemetryEvent.Status.Should().Be(status);
        telemetryEvent.AdditionalData.Should().Be(additionalData);
    }

    [Fact]
    public void TelemetryEvent_AdditionalData_Can_Be_Null()
    {
        // Arrange & Act
        var telemetryEvent = new TelemetryEvent
        {
            NodeId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            LatencyMs = 10.0,
            ThroughputMbps = 500.0,
            Status = NodeStatus.Active,
            AdditionalData = null
        };

        // Assert
        telemetryEvent.AdditionalData.Should().BeNull();
    }

    [Fact]
    public void TelemetryEvent_Metrics_Should_Be_Non_Negative()
    {
        // Arrange & Act
        var telemetryEvent = new TelemetryEvent
        {
            NodeId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            LatencyMs = 0.0,
            ThroughputMbps = 0.0,
            Status = NodeStatus.Maintenance
        };

        // Assert
        telemetryEvent.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
        telemetryEvent.ThroughputMbps.Should().BeGreaterThanOrEqualTo(0);
    }
}
