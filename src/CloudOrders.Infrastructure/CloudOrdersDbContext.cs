using CloudOrders.Application.Abstractions;
using CloudOrders.Domain;
using CloudOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloudOrders.Infrastructure;

public sealed class CloudOrdersDbContext : DbContext, IQuerySource
{
    public CloudOrdersDbContext(DbContextOptions<CloudOrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    async Task<Order?> IQuerySource.GetOrderByIdAsync(Guid id, CancellationToken cancellationToken)
        => await Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudOrdersDbContext).Assembly);
    }
}