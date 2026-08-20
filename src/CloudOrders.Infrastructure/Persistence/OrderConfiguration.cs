using CloudOrders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudOrders.Infrastructure.Persistence;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();
        
        builder.Property(o => o.Version)
            .IsRowVersion();

        builder.OwnsMany(o => o.Items, items =>
        {
            items.ToTable("OrderItems");

            items.WithOwner()
                .HasForeignKey("OrderId");

            items.Property<Guid>("Id");
            items.HasKey("Id");

            items.Property(i => i.ProductId)
                .IsRequired();

            items.Property(i => i.Quantity)
                .IsRequired();

            items.Property(i => i.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });
    }
}