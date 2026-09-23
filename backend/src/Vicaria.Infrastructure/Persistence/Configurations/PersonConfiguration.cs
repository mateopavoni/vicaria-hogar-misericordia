using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("persona");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .HasMaxLength(100)
            .UseCollation("Modern_Spanish_CI_AI")
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasMaxLength(100)
            .UseCollation("Modern_Spanish_CI_AI");

        builder.Property(p => p.Dni)
            .HasMaxLength(20);

        builder.Property(p => p.Phone)
            .HasMaxLength(30);

        builder.Property(p => p.CreatedAt)
            .IsRequired();
    }
}
