using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class MedicationStockMovementConfiguration : IEntityTypeConfiguration<MedicationStockMovement>
{
    public void Configure(EntityTypeBuilder<MedicationStockMovement> builder)
    {
        builder.ToTable("medication_stock_movements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .IsRequired();

        builder.Property(e => e.Quantity)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Supplier)
            .HasMaxLength(200);

        builder.HasOne(e => e.Medication)
            .WithMany()
            .HasForeignKey(e => e.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.MedicationId);
        builder.HasIndex(e => e.Date);
    }
}