using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class LifeStoryEntryConfiguration : IEntityTypeConfiguration<LifeStoryEntry>
{
    public void Configure(EntityTypeBuilder<LifeStoryEntry> builder)
    {
        builder.ToTable("historia_vida_entradas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PersonId).HasColumnName("persona_id").IsRequired();
        builder.Property(e => e.Stage).HasColumnName("etapa").IsRequired();
        builder.Property(e => e.Content).HasColumnName("contenido").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("usuario_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("fecha_creacion").IsRequired();

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.PersonId, e.Stage });
    }
}
