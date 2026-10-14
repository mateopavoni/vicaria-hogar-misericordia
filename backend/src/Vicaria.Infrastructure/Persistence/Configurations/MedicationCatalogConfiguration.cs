using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class MedicationCatalogConfiguration : IEntityTypeConfiguration<MedicationCatalog>
{
    public void Configure(EntityTypeBuilder<MedicationCatalog> builder)
    {
        builder.ToTable("medication_catalogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Stock)
            .IsRequired();

        builder.HasIndex(e => e.Name);
    }
}
