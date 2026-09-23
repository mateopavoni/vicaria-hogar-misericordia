using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacto");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.LastName).HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Address).HasMaxLength(300);

        // un contacto por ficha (0 o 1, opcional)
        builder.HasIndex(c => c.SocialRecordId).IsUnique();

        builder.HasOne(c => c.SocialRecord)
            .WithMany()
            .HasForeignKey(c => c.SocialRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
