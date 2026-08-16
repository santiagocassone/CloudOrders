using Inventory.Application;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.AvailableQuantity).IsRequired();
            entity.Property(x => x.ReservedQuantity).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });
    }
}
