using CloudOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("ProcessedMessages");

        builder.HasKey(x => x.MessageId);

        builder.Property(x => x.MessageId)
            .HasMaxLength(128);

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired();
    }
}