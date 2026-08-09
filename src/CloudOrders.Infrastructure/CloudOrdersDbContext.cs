using CloudOrders.Application.Abstractions;
using CloudOrders.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudOrders.Infrastructure;

public sealed class CloudOrdersDbContext : DbContext, IQuerySource
{
    public CloudOrdersDbContext(DbContextOptions<CloudOrdersDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<User> Users => Set<User>();
    IQueryable<Order> IQuerySource.Orders => Orders;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudOrdersDbContext).Assembly);
    }
}