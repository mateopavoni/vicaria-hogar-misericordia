using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class CasonaStayConfiguration : IEntityTypeConfiguration<CasonaStay>
{
    public void Configure(EntityTypeBuilder<CasonaStay> builder)
    {
        builder.ToTable("estadia_casona");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntryDate)
            .IsRequired();

        builder.Property(e => e.ExitDate);

        builder.Property(e => e.Reason)
            .HasMaxLength(500);

        // una persona puede tener múltiples estadías en la casona (1:N)
        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
