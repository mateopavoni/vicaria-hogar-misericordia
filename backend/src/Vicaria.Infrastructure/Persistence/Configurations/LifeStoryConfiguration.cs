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

        builder.Property(l => l.BeforeCentroBarrial).HasColumnName("before_centro_barrial");
        builder.Property(l => l.BeforeCentroBarrialUpdatedByUserId).HasColumnName("before_centro_barrial_updated_by_user_id");
        builder.Property(l => l.BeforeCentroBarrialUpdatedAt).HasColumnName("before_centro_barrial_updated_at");

        builder.Property(l => l.InCentroBarrial).HasColumnName("in_centro_barrial");
        builder.Property(l => l.InCentroBarrialUpdatedByUserId).HasColumnName("in_centro_barrial_updated_by_user_id");
        builder.Property(l => l.InCentroBarrialUpdatedAt).HasColumnName("in_centro_barrial_updated_at");

        builder.Property(l => l.AfterCentroBarrial).HasColumnName("after_centro_barrial");
        builder.Property(l => l.AfterCentroBarrialUpdatedByUserId).HasColumnName("after_centro_barrial_updated_by_user_id");
        builder.Property(l => l.AfterCentroBarrialUpdatedAt).HasColumnName("after_centro_barrial_updated_at");

        builder.HasOne(l => l.Person)
            .WithOne()
            .HasForeignKey<LifeStory>(l => l.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.BeforeCentroBarrialUpdatedByUser)
            .WithMany()
            .HasForeignKey(l => l.BeforeCentroBarrialUpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.InCentroBarrialUpdatedByUser)
            .WithMany()
            .HasForeignKey(l => l.InCentroBarrialUpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.AfterCentroBarrialUpdatedByUser)
            .WithMany()
            .HasForeignKey(l => l.AfterCentroBarrialUpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}