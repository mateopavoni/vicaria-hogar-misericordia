using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Attendances;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Attendances;

public class AttendanceDailyMedicationScheduleTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_WhenPersonHasActiveSchemes_GeneratesPendingDailySchedulesAndAuditLog()
    {
        using var context = CreateInMemoryDbContext();
        var service = new AttendanceService(context);

        var actorId = Guid.NewGuid();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Juan" };
        var socialRecord = new SocialRecord { Id = Guid.NewGuid(), PersonId = person.Id, Status = SocialRecordStatus.Active };
        var medication = new MedicationCatalog { Id = Guid.NewGuid(), Name = "Risperdal 2mg", Stock = 10 };

        var scheme1 = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "2mg",
            Time = new TimeSpan(8, 0, 0),
            IsActive = true
        };
        var scheme2 = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "1mg",
            Time = new TimeSpan(20, 0, 0),
            IsActive = true
        };

        context.People.Add(person);
        context.SocialRecords.Add(socialRecord);
        context.MedicationCatalogs.Add(medication);
        context.MedicationSchemes.AddRange(scheme1, scheme2);
        await context.SaveChangesAsync();

        var attendanceDate = new DateTime(2026, 10, 19, 10, 30, 0, DateTimeKind.Utc);
        var dto = new CreateAttendanceDto(person.Id, AttendanceActivity.DayCenter, attendanceDate, 0);

        var result = await service.RegisterAsync(dto, actorId);

        Assert.True(result.Success);

        var schedules = await context.DailyMedicationSchedules
            .Where(s => s.PersonId == person.Id && s.Date == attendanceDate.Date)
            .OrderBy(s => s.Time)
            .ToListAsync();

        Assert.Equal(2, schedules.Count);
        Assert.All(schedules, s => Assert.Equal(MedicationIntakeStatus.Pending, s.Status));
        Assert.Equal(new TimeSpan(8, 0, 0), schedules[0].Time);
        Assert.Equal(new TimeSpan(20, 0, 0), schedules[1].Time);

        var auditLog = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Agenda diaria de medicamentos generada por asistencia");
        Assert.NotNull(auditLog);
        Assert.Equal(actorId, auditLog.UserId);
        Assert.Equal($"Person:{person.Id}", auditLog.AffectedEntity);
    }

    [Fact]
    public async Task RegisterAsync_WhenPersonHasInactiveSchemes_DoesNotGenerateDailySchedules()
    {
        using var context = CreateInMemoryDbContext();
        var service = new AttendanceService(context);

        var actorId = Guid.NewGuid();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Carlos" };
        var socialRecord = new SocialRecord { Id = Guid.NewGuid(), PersonId = person.Id, Status = SocialRecordStatus.Active };
        var medication = new MedicationCatalog { Id = Guid.NewGuid(), Name = "Clonazepam", Stock = 5 };

        var inactiveScheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "0.5mg",
            Time = new TimeSpan(21, 0, 0),
            IsActive = false
        };

        context.People.Add(person);
        context.SocialRecords.Add(socialRecord);
        context.MedicationCatalogs.Add(medication);
        context.MedicationSchemes.Add(inactiveScheme);
        await context.SaveChangesAsync();

        var attendanceDate = new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);
        var dto = new CreateAttendanceDto(person.Id, AttendanceActivity.DayCenter, attendanceDate, 0);

        var result = await service.RegisterAsync(dto, actorId);

        Assert.True(result.Success);

        var count = await context.DailyMedicationSchedules.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RegisterAsync_WhenPersonHasNoSchemes_RegistersAttendanceWithoutGeneratingSchedules()
    {
        using var context = CreateInMemoryDbContext();
        var service = new AttendanceService(context);

        var actorId = Guid.NewGuid();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Marta" };
        var socialRecord = new SocialRecord { Id = Guid.NewGuid(), PersonId = person.Id, Status = SocialRecordStatus.Active };

        context.People.Add(person);
        context.SocialRecords.Add(socialRecord);
        await context.SaveChangesAsync();

        var attendanceDate = new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);
        var dto = new CreateAttendanceDto(person.Id, AttendanceActivity.DayCenter, attendanceDate, 0);

        var result = await service.RegisterAsync(dto, actorId);

        Assert.True(result.Success);

        var count = await context.DailyMedicationSchedules.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RegisterAsync_WhenSchedulesAlreadyExistForSameDay_DoesNotDuplicate()
    {
        using var context = CreateInMemoryDbContext();
        var service = new AttendanceService(context);

        var actorId = Guid.NewGuid();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Luis" };
        var socialRecord = new SocialRecord { Id = Guid.NewGuid(), PersonId = person.Id, Status = SocialRecordStatus.Active };
        var medication = new MedicationCatalog { Id = Guid.NewGuid(), Name = "Ibuprofeno", Stock = 20 };

        var scheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "400mg",
            Time = new TimeSpan(12, 0, 0),
            IsActive = true
        };

        var attendanceDate = new DateTime(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        var existingSchedule = new DailyMedicationSchedule
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationSchemeId = scheme.Id,
            MedicationId = medication.Id,
            Date = attendanceDate.Date,
            Time = scheme.Time,
            Dose = scheme.Dose,
            Status = MedicationIntakeStatus.Pending
        };

        context.People.Add(person);
        context.SocialRecords.Add(socialRecord);
        context.MedicationCatalogs.Add(medication);
        context.MedicationSchemes.Add(scheme);
        context.DailyMedicationSchedules.Add(existingSchedule);
        await context.SaveChangesAsync();

        var dto = new CreateAttendanceDto(person.Id, AttendanceActivity.CasaConvivencia, attendanceDate, 0);

        var result = await service.RegisterAsync(dto, actorId);

        Assert.True(result.Success);

        var totalSchedules = await context.DailyMedicationSchedules
            .Where(s => s.PersonId == person.Id && s.Date == attendanceDate.Date)
            .CountAsync();

        Assert.Equal(1, totalSchedules);
    }
}