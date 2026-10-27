using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class PersonStatusChangeConfiguration : IEntityTypeConfiguration<PersonStatusChange>
{
    public void Configure(EntityTypeBuilder<PersonStatusChange> builder)
    {
        builder.ToTable("person_status_changes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.PersonId).HasColumnName("person_id").IsRequired();
        builder.Property(c => c.PreviousStatus)
            .HasColumnName("previous_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(c => c.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(c => c.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(c => c.ChangedByUserId).HasColumnName("user_id").IsRequired(false);
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
