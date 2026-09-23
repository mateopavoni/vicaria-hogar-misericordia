using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class SocialRecordConfiguration : IEntityTypeConfiguration<SocialRecord>
{
    public void Configure(EntityTypeBuilder<SocialRecord> builder)
    {
        builder.ToTable("ficha_social");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.PersonType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.ReasonForEntry).HasMaxLength(500);
        builder.Property(r => r.HousingSituation).HasMaxLength(200);
        builder.Property(r => r.OvernightLocation).HasMaxLength(200);
        builder.Property(r => r.Occupation).HasMaxLength(200);
        builder.Property(r => r.GeneralNotes).HasMaxLength(2000);

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasOne(r => r.Person)
            .WithMany()
            .HasForeignKey(r => r.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
