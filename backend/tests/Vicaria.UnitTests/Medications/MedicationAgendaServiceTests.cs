using Microsoft.EntityFrameworkCore;
using Vicaria.Application.MedicationAgendas;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.MedicationAgendas;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Medications;

public class MedicationAgendaServiceTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task GetDailyAgendaAsync_ReturnsIntakesGroupedByTimeAndOrdered()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationAgendaService(context);

        var person1 = new Person { Id = Guid.NewGuid(), FirstName = "Juan", LastName = "Pérez" };
        var person2 = new Person { Id = Guid.NewGuid(), FirstName = "José", LastName = "Reyes" };
        var med1 = new MedicationCatalog { Id = Guid.NewGuid(), Name = "Risperdal 2mg", Stock = 10 };
        var med2 = new MedicationCatalog { Id = Guid.NewGuid(), Name = "Vitamina D 500mg", Stock = 20 };

        context.People.AddRange(person1, person2);
        context.MedicationCatalogs.AddRange(med1, med2);

        var date = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);

        var intake1 = new DailyMedicationSchedule
        {
            Id = Guid.NewGuid(),
            PersonId = person1.Id,
            MedicationId = med1.Id,
            Date = date,
            Time = new TimeSpan(8, 0, 0),
            Dose = "2mg",
            Status = MedicationIntakeStatus.Pending
        };

        var intake2 = new DailyMedicationSchedule
        {
            Id = Guid.NewGuid(),
            PersonId = person2.Id,
            MedicationId = med2.Id,
            Date = date,
            Time = new TimeSpan(8, 0, 0),
            Dose = "500mg",
            Status = MedicationIntakeStatus.Pending
        };

        var intake3 = new DailyMedicationSchedule
        {
            Id = Guid.NewGuid(),
            PersonId = person1.Id,
            MedicationId = med1.Id,
            Date = date,
            Time = new TimeSpan(20, 0, 0),
            Dose = "2mg",
            Status = MedicationIntakeStatus.Pending
        };

        context.DailyMedicationSchedules.AddRange(intake1, intake2, intake3);
        await context.SaveChangesAsync();

        var result = await service.GetDailyAgendaAsync(date);

        Assert.Equal(date, result.Date);
        Assert.Equal(2, result.Groups.Count);

        var group8am = result.Groups.First(g => g.Time == "08:00");
        Assert.Equal(2, group8am.Intakes.Count);
        Assert.Contains(group8am.Intakes, i => i.PersonFullName == "Juan Pérez" && i.MedicationName == "Risperdal 2mg");
        Assert.Contains(group8am.Intakes, i => i.PersonFullName == "José Reyes" && i.MedicationName == "Vitamina D 500mg");

        var group8pm = result.Groups.First(g => g.Time == "20:00");
        Assert.Single(group8pm.Intakes);
        Assert.Equal("Juan Pérez", group8pm.Intakes[0].PersonFullName);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenIntakeExists_UpdatesStatusObservationAndCreatesAuditLog()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationAgendaService(context);

        var actorId = Guid.NewGuid();
        var intake = new DailyMedicationSchedule
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            MedicationId = Guid.NewGuid(),
            Date = DateTime.UtcNow.Date,
            Time = new TimeSpan(8, 0, 0),
            Dose = "2mg",
            Status = MedicationIntakeStatus.Pending
        };

        context.DailyMedicationSchedules.Add(intake);
        await context.SaveChangesAsync();

        var dto = new UpdateMedicationIntakeStatusDto(MedicationIntakeStatus.Administered, "Tomado con el desayuno");

        var result = await service.UpdateStatusAsync(intake.Id, dto, actorId);

        Assert.True(result.Success);

        var updated = await context.DailyMedicationSchedules.FirstAsync(i => i.Id == intake.Id);
        Assert.Equal(MedicationIntakeStatus.Administered, updated.Status);
        Assert.Equal("Tomado con el desayuno", updated.Observation);
        Assert.NotNull(updated.AdministeredAt);
        Assert.Equal(actorId, updated.AdministeredByUserId);

        var auditLog = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Toma de medicación marcada como Administrada");
        Assert.NotNull(auditLog);
        Assert.Equal(actorId, auditLog.UserId);
        Assert.Equal($"DailyMedicationSchedule:{intake.Id}", auditLog.AffectedEntity);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenIntakeNotFound_ReturnsNotFound()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationAgendaService(context);

        var dto = new UpdateMedicationIntakeStatusDto(MedicationIntakeStatus.NotAdministered, "Rechazó la toma");

        var result = await service.UpdateStatusAsync(Guid.NewGuid(), dto, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(UpdateMedicationIntakeError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenStatusIsPending_ReturnsInvalidStatus()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationAgendaService(context);

        var dto = new UpdateMedicationIntakeStatusDto(MedicationIntakeStatus.Pending, null);

        var result = await service.UpdateStatusAsync(Guid.NewGuid(), dto, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(UpdateMedicationIntakeError.InvalidStatus, result.Error);
    }
}