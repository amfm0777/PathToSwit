namespace TelecomOps.Core;

public enum FrequencyBand
{
    Band3 = 1800,
    Band7 = 2600,
    Band28 = 700,
    Band78 = 3500
}

public enum NodeStatus
{
    Active,
    Inactive,
    Maintenance,
    Faulty
}

public class NodeConfig
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public FrequencyBand FrequencyBand { get; set; }
    public NodeStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}