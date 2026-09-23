using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class CasaConvivenciaStayConfiguration : IEntityTypeConfiguration<CasaConvivenciaStay>
{
    public void Configure(EntityTypeBuilder<CasaConvivenciaStay> builder)
    {
        builder.ToTable("estadia_casa_convivencia");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryDate)
            .IsRequired();

        builder.Property(e => e.ExitDate);

        builder.Property(e => e.ExitReason);

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        // una persona puede tener múltiples estadías en la casa de convivencia (1:N)
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
