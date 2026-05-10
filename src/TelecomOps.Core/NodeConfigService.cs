namespace TelecomOps.Core;

public interface INodeConfigService
{
    Task<NodeConfig> CreateNodeConfigAsync(string name, FrequencyBand frequencyBand);
    Task UpdateNodeStatusAsync(Guid id, NodeStatus status);
}

public class NodeConfigService : INodeConfigService
{
    private readonly INodeConfigRepository _repository;

    public NodeConfigService(INodeConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<NodeConfig> CreateNodeConfigAsync(string name, FrequencyBand frequencyBand)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        var nodeConfig = new NodeConfig
        {
            Id = Guid.NewGuid(),
            Name = name,
            FrequencyBand = frequencyBand,
            Status = NodeStatus.Inactive,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(nodeConfig);
        return nodeConfig;
    }

    public async Task UpdateNodeStatusAsync(Guid id, NodeStatus status)
    {
        var nodeConfig = await _repository.GetByIdAsync(id);
        if (nodeConfig == null)
            throw new KeyNotFoundException($"NodeConfig with id {id} not found");

        nodeConfig.Status = status;
        nodeConfig.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(nodeConfig);
    }
}