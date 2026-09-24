using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class PersonTypeChangeConfiguration : IEntityTypeConfiguration<PersonTypeChange>
{
    public void Configure(EntityTypeBuilder<PersonTypeChange> builder)
    {
        builder.ToTable("historial_tipo_persona");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.PersonId).HasColumnName("persona_id").IsRequired();
        builder.Property(c => c.PreviousType).HasColumnName("tipo_anterior");
        builder.Property(c => c.NewType).HasColumnName("tipo_nuevo").IsRequired();
        builder.Property(c => c.ChangedByUserId).HasColumnName("usuario_id").IsRequired();
        builder.Property(c => c.ChangedAt).HasColumnName("fecha_cambio").IsRequired();

        builder.HasOne(c => c.Person)
            .WithMany()
            .HasForeignKey(c => c.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ChangedByUser)
            .WithMany()
            .HasForeignKey(c => c.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.PersonId);
    }
}
