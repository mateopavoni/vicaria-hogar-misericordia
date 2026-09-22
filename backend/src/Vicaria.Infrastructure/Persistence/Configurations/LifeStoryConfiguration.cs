using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class LifeStoryConfiguration : IEntityTypeConfiguration<LifeStory>
{
    public void Configure(EntityTypeBuilder<LifeStory> builder)
    {
        builder.ToTable("historia_vida");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.PersonId).HasColumnName("persona_id").IsRequired();

        builder.Property(l => l.BeforeHogar).HasColumnName("antes_hogar");
        builder.Property(l => l.BeforeHogarUpdatedByUserId).HasColumnName("antes_hogar_usuario_id");
        builder.Property(l => l.BeforeHogarUpdatedAt).HasColumnName("antes_hogar_fecha_edicion");

        builder.Property(l => l.InHogar).HasColumnName("en_hogar");
        builder.Property(l => l.InHogarUpdatedByUserId).HasColumnName("en_hogar_usuario_id");
        builder.Property(l => l.InHogarUpdatedAt).HasColumnName("en_hogar_fecha_edicion");

        builder.Property(l => l.AfterHogar).HasColumnName("despues_hogar");
        builder.Property(l => l.AfterHogarUpdatedByUserId).HasColumnName("despues_hogar_usuario_id");
        builder.Property(l => l.AfterHogarUpdatedAt).HasColumnName("despues_hogar_fecha_edicion");

        builder.HasOne(l => l.Person)
            .WithOne()
            .HasForeignKey<LifeStory>(l => l.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.BeforeHogarUpdatedByUser)
            .WithMany()
            .HasForeignKey(l => l.BeforeHogarUpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.InHogarUpdatedByUser)
            .WithMany()
            .HasForeignKey(l => l.InHogarUpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.AfterHogarUpdatedByUser)
            .WithMany()
            .HasForeignKey(l => l.AfterHogarUpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}