namespace TelecomOps.Core;

public interface INodeConfigRepository
{
    Task<NodeConfig?> GetByIdAsync(Guid id);
    Task<IEnumerable<NodeConfig>> GetAllAsync();
    Task<IEnumerable<NodeConfig>> GetActiveNodesAsync();
    Task AddAsync(NodeConfig nodeConfig);
    Task UpdateAsync(NodeConfig nodeConfig);
    Task DeleteAsync(Guid id);
}