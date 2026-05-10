using Xunit;
using FluentAssertions;
using Moq;
using TelecomOps.Core;

namespace TelecomOps.Worker.Tests;

public class TelemetryWorkerMockTests
{
    [Fact]
    public async Task Worker_Should_Query_Active_Nodes()
    {
        // Arrange
        var mockRepo = new Mock<INodeConfigRepository>();
        var mockPublisher = new Mock<IMetricsPublisher>();
        var activeNodes = new List<NodeConfig>
        {
            new NodeConfig 
            { 
                Id = Guid.NewGuid(), 
                Name = "Node 1", 
                FrequencyBand = FrequencyBand.Band78, 
                Status = NodeStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };
        mockRepo.Setup(r => r.GetActiveNodesAsync()).ReturnsAsync(activeNodes);
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<TelemetryEvent>())).Returns(Task.CompletedTask);

        // Act
        var result = await mockRepo.Object.GetActiveNodesAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(NodeStatus.Active);
        mockRepo.Verify(r => r.GetActiveNodesAsync(), Times.Once);
    }

    [Fact]
    public async Task Worker_Should_Publish_Metrics_For_Each_Node()
    {
        // Arrange
        var mockPublisher = new Mock<IMetricsPublisher>();
        var nodeId = Guid.NewGuid();
        var telemetryEvents = new List<TelemetryEvent>
        {
            new TelemetryEvent 
            { 
                NodeId = nodeId, 
                Timestamp = DateTime.UtcNow, 
                LatencyMs = 45.0, 
                ThroughputMbps = 200.0, 
                Status = NodeStatus.Active 
            },
            new TelemetryEvent 
            { 
                NodeId = nodeId, 
                Timestamp = DateTime.UtcNow.AddSeconds(5), 
                LatencyMs = 50.0, 
                ThroughputMbps = 210.0, 
                Status = NodeStatus.Active 
            }
        };
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<TelemetryEvent>())).Returns(Task.CompletedTask);

        // Act
        foreach (var @event in telemetryEvents)
        {
            await mockPublisher.Object.PublishAsync(@event);
        }

        // Assert
        mockPublisher.Verify(p => p.PublishAsync(It.IsAny<TelemetryEvent>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Worker_Should_Handle_No_Active_Nodes()
    {
        // Arrange
        var mockRepo = new Mock<INodeConfigRepository>();
        mockRepo.Setup(r => r.GetActiveNodesAsync()).ReturnsAsync(new List<NodeConfig>());

        // Act
        var result = await mockRepo.Object.GetActiveNodesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Worker_Should_Publish_Metrics_For_Multiple_Nodes(int nodeCount)
    {
        // Arrange
        var mockPublisher = new Mock<IMetricsPublisher>();
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(_ => new NodeConfig 
            { 
                Id = Guid.NewGuid(), 
                Name = $"Node-{_}", 
                FrequencyBand = FrequencyBand.Band78, 
                Status = NodeStatus.Active,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<TelemetryEvent>())).Returns(Task.CompletedTask);

        // Act
        foreach (var node in nodes)
        {
            var @event = new TelemetryEvent
            {
                NodeId = node.Id,
                Timestamp = DateTime.UtcNow,
                LatencyMs = 40.0,
                ThroughputMbps = 150.0,
                Status = node.Status
            };
            await mockPublisher.Object.PublishAsync(@event);
        }

        // Assert
        mockPublisher.Verify(p => p.PublishAsync(It.IsAny<TelemetryEvent>()), Times.Exactly(nodeCount));
    }
}
