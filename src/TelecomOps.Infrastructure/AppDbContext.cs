using Microsoft.EntityFrameworkCore;
using TelecomOps.Core;

namespace TelecomOps.Infrastructure;

/// <summary>
/// Represents the application's database context, providing access to the NodeConfig entities.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<NodeConfig> NodeConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NodeConfig>(entity =>
        {
            entity.ToTable("nodeconfigs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.FrequencyBand).IsRequired().HasColumnName("frequency_band");
            entity.Property(e => e.Status).IsRequired().HasColumnName("status");
            entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
    }
}