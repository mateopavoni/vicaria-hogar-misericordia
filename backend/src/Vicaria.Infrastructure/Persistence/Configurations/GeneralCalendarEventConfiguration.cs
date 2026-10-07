using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class GeneralCalendarEventConfiguration : IEntityTypeConfiguration<GeneralCalendarEvent>
{
    // ids fijos para que el seed sea determinístico entre entornos (SCRUM-183)
    private static readonly Guid BreakfastId = new("88888888-8888-8888-8888-888888888801");
    private static readonly Guid LunchId = new("88888888-8888-8888-8888-888888888802");
    private static readonly Guid SnackId = new("88888888-8888-8888-8888-888888888803");
    private static readonly DateTime BaseDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private const WeekDays Weekdays =
        WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday | WeekDays.Friday;

    public void Configure(EntityTypeBuilder<GeneralCalendarEvent> builder)
    {
        builder.ToTable("general_calendar_events");

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

        builder.Property(e => e.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(e => e.AuthorUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new GeneralCalendarEvent
            {
                Id = BreakfastId,
                Title = "Desayuno",
                Date = null,
                StartTime = new TimeSpan(9, 30, 0),
                EndTime = new TimeSpan(11, 0, 0),
                RecurrenceDays = Weekdays,
                AuthorUserId = null,
                CreatedAt = BaseDate
            },
            new GeneralCalendarEvent
            {
                Id = LunchId,
                Title = "Almuerzo",
                Date = null,
                StartTime = new TimeSpan(13, 30, 0),
                EndTime = new TimeSpan(14, 30, 0),
                RecurrenceDays = Weekdays,
                AuthorUserId = null,
                CreatedAt = BaseDate
            },
            new GeneralCalendarEvent
            {
                Id = SnackId,
                Title = "Merendero",
                Date = null,
                StartTime = new TimeSpan(16, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                RecurrenceDays = WeekDays.Tuesday,
                AuthorUserId = null,
                CreatedAt = BaseDate
            }
        );
    }
}
