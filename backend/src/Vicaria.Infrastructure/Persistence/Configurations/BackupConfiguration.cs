using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class BackupConfiguration : IEntityTypeConfiguration<Backup>
{
    public void Configure(EntityTypeBuilder<Backup> builder)
    {
        builder.ToTable("backups");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.SizeBytes)
            .IsRequired();

        builder.Property(b => b.ExternalKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasIndex(b => b.CreatedAt);
    }
}
