using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class MedicationSchemeConfiguration : IEntityTypeConfiguration<MedicationScheme>
{
    public void Configure(EntityTypeBuilder<MedicationScheme> builder)
    {
        builder.ToTable("medication_schemes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Dose)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Time)
            .IsRequired();

        builder.Property(e => e.LoadedAt)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.PendingStock)
            .IsRequired();

        // 1:N — una persona puede tener varios esquemas
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:N — un medicamento del catálogo puede estar en varios esquemas;
        // no se borra el catálogo si tiene esquemas cargados
        builder.HasOne(e => e.Medication)
            .WithMany()
            .HasForeignKey(e => e.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:N — un usuario puede cargar esquemas de distintas personas;
        // no se borran esquemas si se borra el usuario
        builder.HasOne(e => e.LoadedBy)
            .WithMany()
            .HasForeignKey(e => e.LoadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:N — usuario que revisó por última vez (SCRUM-249); opcional y
        // Restrict por el mismo motivo que LoadedBy
        builder.HasOne(e => e.ReviewedBy)
            .WithMany()
            .HasForeignKey(e => e.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.PersonId);
        builder.HasIndex(e => e.MedicationId);
        builder.HasIndex(e => e.ReviewedByUserId);
    }
}
