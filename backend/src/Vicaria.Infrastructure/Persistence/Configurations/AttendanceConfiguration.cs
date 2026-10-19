using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("attendances");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Activity)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.AnonymousCount)
            .IsRequired();

        // justificación de la modificación de un día cerrado (SCRUM-217):
        // opcional a nivel de BD (solo obligatoria por regla de negocio si el día está cerrado)
        builder.Property(e => e.Justification)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        // índice único compuesto (personaId, actividad, fecha)
        builder.HasIndex(e => new { e.PersonId, e.Activity, e.Date })
            .IsUnique()
            .HasDatabaseName("IX_attendances_PersonId_Activity_Date");

        // una persona puede tener múltiples registros de asistencia (1:N)
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}