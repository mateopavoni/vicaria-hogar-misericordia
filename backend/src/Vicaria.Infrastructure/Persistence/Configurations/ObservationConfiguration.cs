using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("observacion");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.PersonId)
            .HasColumnName("persona_id")
            .IsRequired();

        builder.Property(o => o.Content)
            .HasColumnName("contenido")
            .IsRequired();

        builder.Property(o => o.CategoryId)
            .HasColumnName("categoria_id")
            .IsRequired(false);

        builder.Property(o => o.AuthorUserId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("fecha_creacion")
            .IsRequired();

        builder.HasOne(o => o.Person)
            .WithMany()
            .HasForeignKey(o => o.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Category)
            .WithMany()
            .HasForeignKey(o => o.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.AuthorUser)
            .WithMany()
            .HasForeignKey(o => o.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}