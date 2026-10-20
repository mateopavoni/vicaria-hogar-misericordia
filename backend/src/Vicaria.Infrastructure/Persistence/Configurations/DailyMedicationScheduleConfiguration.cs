using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class DailyMedicationScheduleConfiguration : IEntityTypeConfiguration<DailyMedicationSchedule>
{
    public void Configure(EntityTypeBuilder<DailyMedicationSchedule> builder)
    {
        builder.ToTable("daily_medication_schedules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Time)
            .IsRequired();

        builder.Property(e => e.Dose)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.Observation)
            .HasMaxLength(500);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.MedicationScheme)
            .WithMany()
            .HasForeignKey(e => e.MedicationSchemeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Medication)
            .WithMany()
            .HasForeignKey(e => e.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AdministeredByUser)
            .WithMany()
            .HasForeignKey(e => e.AdministeredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.PersonId);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => new { e.PersonId, e.Date });
    }
}