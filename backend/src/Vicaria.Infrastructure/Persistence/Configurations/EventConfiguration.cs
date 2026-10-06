using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("evento");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Title).HasColumnName("titulo").HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasColumnName("descripcion");
        builder.Property(e => e.StartDate).HasColumnName("fecha_inicio").IsRequired();
        builder.Property(e => e.EndDate).HasColumnName("fecha_fin").IsRequired();
        builder.Property(e => e.IsRecurring).HasColumnName("es_recurrente").IsRequired();
        builder.Property(e => e.RecurrencePattern).HasColumnName("patron_recurrencia").IsRequired();
        builder.Property(e => e.RecurrenceUntil).HasColumnName("recurrencia_hasta");
        builder.Property(e => e.AuthorUserId).HasColumnName("usuario_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("fecha_creacion").IsRequired();

        builder.Property(e => e.RecurrenceDays)
            .HasColumnName("dias_recurrencia")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<DayOfWeek[]>(v, (JsonSerializerOptions?)null)
            );

        builder.HasOne(e => e.AuthorUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}