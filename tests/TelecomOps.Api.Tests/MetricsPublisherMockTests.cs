                                                                                                                                                                                using Xunit;
using FluentAssertions;
using Moq;
using TelecomOps.Core;

namespace TelecomOps.Api.Tests;

public class MetricsPublisherMockTests
{
    [Fact]
    public async Task PublishAsync_Should_Be_Called_With_Valid_Event()
    {
        // Arrange
        var mockPublisher = new Mock<IMetricsPublisher>();
        var telemetryEvent = new TelemetryEvent
        {
            NodeId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            LatencyMs = 50.5,
            ThroughputMbps = 200.0,
            Status = NodeStatus.Active
        };
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<TelemetryEvent>())).Returns(Task.CompletedTask);

        // Act
        await mockPublisher.Object.PublishAsync(telemetryEvent);

        // Assert
        mockPublisher.Verify(p => p.PublishAsync(It.Is<TelemetryEvent>(
            e => e.NodeId == telemetryEvent.NodeId && e.LatencyMs == telemetryEvent.LatencyMs
        )), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_Should_Handle_Multiple_Events()
    {
        // Arrange
        var mockPublisher = new Mock<IMetricsPublisher>();
        var events = new List<TelemetryEvent>
        {
            new TelemetryEvent 
            { 
                NodeId = Guid.NewGuid(), 
                Timestamp = DateTime.UtcNow, 
                LatencyMs = 10.0, 
                ThroughputMbps = 100.0, 
                Status = NodeStatus.Active 
            },
            new TelemetryEvent 
            { 
                NodeId = Guid.NewGuid(), 
                Timestamp = DateTime.UtcNow, 
                LatencyMs = 20.0, 
                ThroughputMbps = 200.0, 
                Status = NodeStatus.Inactive 
            }
        };
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<TelemetryEvent>())).Returns(Task.CompletedTask);

        // Act
        foreach (var @event in events)
        {
            await mockPublisher.Object.PublishAsync(@event);
        }

        // Assert
        mockPublisher.Verify(p => p.PublishAsync(It.IsAny<TelemetryEvent>()), Times.Exactly(2));
    }
}
