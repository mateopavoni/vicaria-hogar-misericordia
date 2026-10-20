using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.LinkUrl)
            .HasMaxLength(500);

        builder.Property(n => n.IsRead)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.TargetRole)
            .HasMaxLength(50);

        builder.HasIndex(n => new { n.TargetRole, n.IsRead });
    }
}
