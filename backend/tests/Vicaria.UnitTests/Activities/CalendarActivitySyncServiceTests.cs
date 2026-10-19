using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Vicaria.Api.Controllers;
using Vicaria.Application.Activities;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Activities;
using Vicaria.Infrastructure.Attendances;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Activities;

public class CalendarActivitySyncServiceTests
{
    private static VicariaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new VicariaDbContext(options);
        db.Database.EnsureCreated();

        db.GeneralCalendarEvents.RemoveRange(db.GeneralCalendarEvents);
        db.Activities.RemoveRange(db.Activities);
        db.SaveChanges();

        return db;
    }

    [Fact]
    public async Task SyncRecurringActivitiesAsync_EventosRecurrentes_GeneraActividadesYAuditLog()
    {
        using var db = CrearDbContext();
        var actorId = Guid.NewGuid();

        db.GeneralCalendarEvents.Add(new GeneralCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Merendero Comunitario",
            RecurrenceDays = WeekDays.Tuesday,
            CreatedAt = DateTime.UtcNow
        });

        db.GeneralCalendarEvents.Add(new GeneralCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Taller de Grupo de Escucha",
            Date = null,
            CreatedAt = DateTime.UtcNow
        });

        db.GeneralCalendarEvents.Add(new GeneralCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Evento Único",
            Date = DateTime.UtcNow.AddDays(2),
            RecurrenceDays = WeekDays.None,
            RepeatsMonthly = false,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new CalendarActivitySyncService(db);

        var result = await service.SyncRecurringActivitiesAsync(actorId);

        Assert.Equal(2, result.SyncedCount);
        Assert.Contains("Merendero Comunitario", result.SyncedActivityNames);
        Assert.Contains("Taller de Grupo de Escucha", result.SyncedActivityNames);

        var actividades = await db.Activities.Where(a => a.IsFromCalendar).ToListAsync();
        Assert.Equal(2, actividades.Count);
        Assert.All(actividades, a => Assert.True(a.IsActive));

        var auditLogs = await db.AuditLogs.Where(l => l.Action.Contains("sincronizada desde calendario")).ToListAsync();
        Assert.Equal(2, auditLogs.Count);
    }

    [Fact]
    public async Task SyncRecurringActivitiesAsync_ActividadYaExistente_NoLaDuplica()
    {
        using var db = CrearDbContext();
        var actorId = Guid.NewGuid();

        db.Activities.Add(new Activity
        {
            Id = Guid.NewGuid(),
            Name = "Merendero Comunitario",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        db.GeneralCalendarEvents.Add(new GeneralCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Merendero Comunitario",
            RecurrenceDays = WeekDays.Tuesday,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new CalendarActivitySyncService(db);

        var result = await service.SyncRecurringActivitiesAsync(actorId);

        Assert.Equal(0, result.SyncedCount);
        Assert.Single(await db.Activities.ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_MismaPersonaMismoDiaDistintasActividades_PermiteAmbosRegistros()
    {
        using var db = CrearDbContext();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Marcos", CreatedAt = DateTime.UtcNow };
        db.People.Add(person);

        db.SocialRecords.Add(new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            PersonType = PersonType.Ambulatory,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AttendanceService(db);
        var today = DateTime.UtcNow.Date;

        var r1 = await service.RegisterAsync(new CreateAttendanceDto(person.Id, AttendanceActivity.DayCenter, today), Guid.NewGuid());
        var r2 = await service.RegisterAsync(new CreateAttendanceDto(person.Id, AttendanceActivity.CasaConvivencia, today), Guid.NewGuid());

        Assert.True(r1.Success);
        Assert.True(r2.Success);
        Assert.Equal(2, await db.Attendances.CountAsync(a => a.PersonId == person.Id));
    }

    [Fact]
    public void ActivitiesController_SincronizarCalendario_RestringidoAReferente()
    {
        var method = typeof(ActivitiesController).GetMethod(nameof(ActivitiesController.SyncCalendarActivities));
        var authAttr = method?.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttr);
        Assert.Equal(RoleNames.Referent, authAttr.Roles);
    }
}