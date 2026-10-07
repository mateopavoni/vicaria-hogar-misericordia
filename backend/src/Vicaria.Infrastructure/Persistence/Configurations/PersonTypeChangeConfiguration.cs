using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class PersonTypeChangeConfiguration : IEntityTypeConfiguration<PersonTypeChange>
{
    public void Configure(EntityTypeBuilder<PersonTypeChange> builder)
    {
        builder.ToTable("person_type_changes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.PersonId).HasColumnName("person_id").IsRequired();
        builder.Property(c => c.PreviousType).HasColumnName("previous_type");
        builder.Property(c => c.NewType).HasColumnName("new_type").IsRequired();
        builder.Property(c => c.ChangedByUserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.ChangedAt).HasColumnName("changed_at").IsRequired();

        builder.HasOne(c => c.Person)
            .WithMany()
            .HasForeignKey(c => c.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ChangedByUser)
            .WithMany()
            .HasForeignKey(c => c.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.PersonId);
    }
}
