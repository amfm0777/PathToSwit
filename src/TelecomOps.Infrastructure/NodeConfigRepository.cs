using Microsoft.EntityFrameworkCore;
using TelecomOps.Core;

namespace TelecomOps.Infrastructure;

public class NodeConfigRepository : INodeConfigRepository
{
    private readonly AppDbContext _context;

    public NodeConfigRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NodeConfig?> GetByIdAsync(Guid id)
    {
        return await _context.NodeConfigs.FindAsync(id);
    }

    public async Task<IEnumerable<NodeConfig>> GetAllAsync()
    {
        return await _context.NodeConfigs.ToListAsync();
    }

    public async Task<IEnumerable<NodeConfig>> GetActiveNodesAsync()
    {
        return await _context.NodeConfigs.Where(n => n.Status == NodeStatus.Active).ToListAsync();
    }

    public async Task AddAsync(NodeConfig nodeConfig)
    {
        await _context.NodeConfigs.AddAsync(nodeConfig);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(NodeConfig nodeConfig)
    {
        _context.NodeConfigs.Update(nodeConfig);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var nodeConfig = await GetByIdAsync(id);
        if (nodeConfig != null)
        {
            _context.NodeConfigs.Remove(nodeConfig);
            await _context.SaveChangesAsync();
        }
    }
}