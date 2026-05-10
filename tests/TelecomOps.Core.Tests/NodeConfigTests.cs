using Xunit;
using FluentAssertions;
using TelecomOps.Core;

namespace TelecomOps.Core.Tests;

public class NodeConfigTests
{
    [Fact]
    public void NodeConfig_Creation_Should_Set_Properties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Node";
        var frequencyBand = FrequencyBand.Band78;
        var status = NodeStatus.Active;
        var createdAt = DateTime.UtcNow;

        // Act
        var nodeConfig = new NodeConfig
        {
            Id = id,
            Name = name,
            FrequencyBand = frequencyBand,
            Status = status,
            CreatedAt = createdAt
        };

        // Assert
        nodeConfig.Id.Should().Be(id);
        nodeConfig.Name.Should().Be(name);
        nodeConfig.FrequencyBand.Should().Be(frequencyBand);
        nodeConfig.Status.Should().Be(status);
        nodeConfig.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void NodeConfig_UpdatedAt_Can_Be_Null()
    {
        // Arrange & Act
        var nodeConfig = new NodeConfig
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            FrequencyBand = FrequencyBand.Band7,
            Status = NodeStatus.Inactive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        // Assert
        nodeConfig.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(FrequencyBand.Band3)]
    [InlineData(FrequencyBand.Band7)]
    [InlineData(FrequencyBand.Band28)]
    [InlineData(FrequencyBand.Band78)]
    public void NodeConfig_Should_Support_All_FrequencyBands(FrequencyBand band)
    {
        // Arrange & Act
        var nodeConfig = new NodeConfig
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            FrequencyBand = band,
            Status = NodeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        nodeConfig.FrequencyBand.Should().Be(band);
    }

    [Theory]
    [InlineData(NodeStatus.Active)]
    [InlineData(NodeStatus.Inactive)]
    [InlineData(NodeStatus.Maintenance)]
    [InlineData(NodeStatus.Faulty)]
    public void NodeConfig_Should_Support_All_Statuses(NodeStatus status)
    {
        // Arrange & Act
        var nodeConfig = new NodeConfig
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            FrequencyBand = FrequencyBand.Band78,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        nodeConfig.Status.Should().Be(status);
    }
}
