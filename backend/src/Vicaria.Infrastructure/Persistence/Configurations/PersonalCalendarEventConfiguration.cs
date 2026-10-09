using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class PersonalCalendarEventConfiguration : IEntityTypeConfiguration<PersonalCalendarEvent>
{
    public void Configure(EntityTypeBuilder<PersonalCalendarEvent> builder)
    {
        builder.ToTable("personal_calendar_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.Date)
            .HasColumnName("date")
            .IsRequired(false);

        builder.Property(e => e.StartTime)
            .HasColumnName("start_time")
            .IsRequired(false);

        builder.Property(e => e.EndTime)
            .HasColumnName("end_time")
            .IsRequired(false);

        builder.Property(e => e.RecurrenceDays)
            .HasColumnName("recurrence_days")
            .IsRequired();

        builder.Property(e => e.RepeatsMonthly)
            .HasColumnName("repeats_monthly")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(e => e.AuthorUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
