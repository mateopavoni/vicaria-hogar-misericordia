using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class ObservationCategoryConfiguration : IEntityTypeConfiguration<ObservationCategory>
{
    public void Configure(EntityTypeBuilder<ObservationCategory> builder)
    {
        builder.ToTable("categoria_observacion");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();
            var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new ObservationCategory
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                Name = "Salud",
                IsActive = true,
                CreatedAt = baseDate
            },
            new ObservationCategory
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                Name = "Documentación",
                IsActive = true,
                CreatedAt = baseDate
            },
            new ObservationCategory
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                Name = "Situación habitacional",
                IsActive = true,
                CreatedAt = baseDate
            },
            new ObservationCategory
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
                Name = "Consumo",
                IsActive = true,
                CreatedAt = baseDate
            },
            new ObservationCategory
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
                Name = "Judicial",
                IsActive = true,
                CreatedAt = baseDate
            },
            new ObservationCategory
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
                Name = "General",
                IsActive = true,
                CreatedAt = baseDate
            }
        );
    }   
}