using Xunit;
using FluentAssertions;
using Moq;
using TelecomOps.Core;

namespace TelecomOps.Api.Tests;

public class NodeConfigRepositoryMockTests
{
    [Fact]
    public async Task GetAllAsync_Should_Return_All_Nodes()
    {
        // Arrange
        var mockRepo = new Mock<INodeConfigRepository>();
        var nodes = new List<NodeConfig>
        {
            new NodeConfig 
            { 
                Id = Guid.NewGuid(), 
                Name = "Node 1", 
                FrequencyBand = FrequencyBand.Band78, 
                Status = NodeStatus.Active,
                CreatedAt = DateTime.UtcNow
            },
            new NodeConfig 
            { 
                Id = Guid.NewGuid(), 
                Name = "Node 2", 
                FrequencyBand = FrequencyBand.Band7, 
                Status = NodeStatus.Inactive,
                CreatedAt = DateTime.UtcNow
            }
        };
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(nodes);

        // Act
        var result = await mockRepo.Object.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Name == "Node 1");
        result.Should().Contain(n => n.Name == "Node 2");
        mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetActiveNodesAsync_Should_Return_Only_Active_Nodes()
    {
        // Arrange
        var mockRepo = new Mock<INodeConfigRepository>();
        var activeNodes = new List<NodeConfig>
        {
            new NodeConfig 
            { 
                Id = Guid.NewGuid(), 
                Name = "Active Node", 
                FrequencyBand = FrequencyBand.Band78, 
                Status = NodeStatus.Active,
                CreatedAt = DateTime.UtcNow
            }
        };
        mockRepo.Setup(r => r.GetActiveNodesAsync()).ReturnsAsync(activeNodes);

        // Act
        var result = await mockRepo.Object.GetActiveNodesAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(NodeStatus.Active);
    }

    [Fact]
    public async Task AddAsync_Should_Call_Successfully()
    {
        // Arrange
        var mockRepo = new Mock<INodeConfigRepository>();
        var newNode = new NodeConfig 
        { 
            Id = Guid.NewGuid(), 
            Name = "New Node", 
            FrequencyBand = FrequencyBand.Band3, 
            Status = NodeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        mockRepo.Setup(r => r.AddAsync(It.IsAny<NodeConfig>())).Returns(Task.CompletedTask);

        // Act
        await mockRepo.Object.AddAsync(newNode);

        // Assert
        mockRepo.Verify(r => r.AddAsync(It.Is<NodeConfig>(n => n.Id == newNode.Id)), Times.Once);
    }
}
