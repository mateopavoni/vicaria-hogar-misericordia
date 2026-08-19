using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Accion)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.EntidadAfectada)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Fecha)
            .IsRequired();
    }
}
