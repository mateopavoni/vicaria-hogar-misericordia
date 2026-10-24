using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class CaritasCatalogValueConfiguration : IEntityTypeConfiguration<CaritasCatalogValue>
{
    public void Configure(EntityTypeBuilder<CaritasCatalogValue> builder)
    {
        builder.ToTable("caritas_catalog_values");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(v => v.IsActive)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .IsRequired();

        builder.HasIndex(v => new { v.Category, v.Name });

        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // valores de ejemplo de SCRUM-70; el resto se carga por el CRUD (SCRUM-274)
        builder.HasData(
            new CaritasCatalogValue
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222201"),
                Category = CaritasCatalogCategory.HousingSituation,
                Name = "En situación de calle",
                IsActive = true,
                CreatedAt = baseDate
            },
            new CaritasCatalogValue
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222202"),
                Category = CaritasCatalogCategory.HousingSituation,
                Name = "Con domicilio",
                IsActive = true,
                CreatedAt = baseDate
            },
            new CaritasCatalogValue
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222203"),
                Category = CaritasCatalogCategory.HousingSituation,
                Name = "Transitorio",
                IsActive = true,
                CreatedAt = baseDate
            },
            new CaritasCatalogValue
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222204"),
                Category = CaritasCatalogCategory.DocumentationStatus,
                Name = "Con DNI",
                IsActive = true,
                CreatedAt = baseDate
            },
            new CaritasCatalogValue
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222205"),
                Category = CaritasCatalogCategory.DocumentationStatus,
                Name = "Sin DNI",
                IsActive = true,
                CreatedAt = baseDate
            },
            new CaritasCatalogValue
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222206"),
                Category = CaritasCatalogCategory.DocumentationStatus,
                Name = "En trámite",
                IsActive = true,
                CreatedAt = baseDate
            }
        );
    }
}
