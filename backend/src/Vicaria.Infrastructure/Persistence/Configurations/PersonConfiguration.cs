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
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasMaxLength(100);

        builder.Property(p => p.Dni)
            .HasMaxLength(20);

        builder.Property(p => p.CreatedAt)
            .IsRequired();
    }
}
