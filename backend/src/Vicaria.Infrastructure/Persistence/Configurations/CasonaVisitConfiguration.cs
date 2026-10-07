using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class CasonaVisitConfiguration : IEntityTypeConfiguration<CasonaVisit>
{
    public void Configure(EntityTypeBuilder<CasonaVisit> builder)
    {
        builder.ToTable("casona_visits");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PersonId).HasColumnName("person_id").IsRequired();
        builder.Property(e => e.VisitorName).HasColumnName("visitor_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Date).HasColumnName("date").IsRequired();
        builder.Property(e => e.StartTime).HasColumnName("start_time").IsRequired();
        builder.Property(e => e.EstimatedDurationMinutes).HasColumnName("estimated_duration_minutes").IsRequired();
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(e => e.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        // una persona puede tener múltiples visitas (1:N); al borrar la persona se borran sus visitas
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // el usuario creador no se borra mientras queden visitas suyas (patrón de todas las FK a User)
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
