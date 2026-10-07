using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class PersonalCalendarEventConfiguration : IEntityTypeConfiguration<PersonalCalendarEvent>
{
    public void Configure(EntityTypeBuilder<PersonalCalendarEvent> builder)
    {
        builder.ToTable("evento_personal");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.Date)
            .IsRequired(false);

        builder.Property(e => e.StartTime)
            .IsRequired(false);

        builder.Property(e => e.EndTime)
            .IsRequired(false);

        builder.Property(e => e.RecurrenceDays)
            .IsRequired();

        builder.Property(e => e.AuthorUserId)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.AuthorUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
