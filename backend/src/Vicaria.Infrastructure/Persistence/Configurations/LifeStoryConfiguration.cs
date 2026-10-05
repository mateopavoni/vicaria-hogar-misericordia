using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class LifeStoryConfiguration : IEntityTypeConfiguration<LifeStory>
{
    public void Configure(EntityTypeBuilder<LifeStory> builder)
    {
        builder.ToTable("life_stories");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.PersonId).HasColumnName("person_id").IsRequired();

        builder.Property(l => l.BeforeHogar).HasColumnName("before_hogar");
        builder.Property(l => l.BeforeHogarUpdatedByUserId).HasColumnName("before_hogar_updated_by_user_id");
        builder.Property(l => l.BeforeHogarUpdatedAt).HasColumnName("before_hogar_updated_at");

        builder.Property(l => l.InHogar).HasColumnName("in_hogar");
        builder.Property(l => l.InHogarUpdatedByUserId).HasColumnName("in_hogar_updated_by_user_id");
        builder.Property(l => l.InHogarUpdatedAt).HasColumnName("in_hogar_updated_at");

        builder.Property(l => l.AfterHogar).HasColumnName("after_hogar");
        builder.Property(l => l.AfterHogarUpdatedByUserId).HasColumnName("after_hogar_updated_by_user_id");
        builder.Property(l => l.AfterHogarUpdatedAt).HasColumnName("after_hogar_updated_at");

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