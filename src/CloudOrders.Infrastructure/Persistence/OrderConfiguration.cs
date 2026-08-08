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

        builder.Property(o => o.Total)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();
    }
}