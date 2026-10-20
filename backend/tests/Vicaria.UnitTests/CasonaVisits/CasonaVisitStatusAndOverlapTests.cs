using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CasonaVisits;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CasonaVisits;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CasonaVisits;

public class CasonaVisitStatusAndOverlapTests
{
    private static VicariaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new VicariaDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<(Guid ActorId, Guid PersonId)> SeedDependenciesAsync(VicariaDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Coordinador",
            LastName = "Casona",
            Email = $"{Guid.NewGuid()}@mail.com",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);

        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Juan",
            LastName = "Pérez",
            Dni = "12345678",
            CreatedAt = DateTime.UtcNow
        };
        db.People.Add(person);

        var record = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PersonType = PersonType.Resident,
            CreatedAt = DateTime.UtcNow
        };
        db.SocialRecords.Add(record);

        var stay = new CasaConvivenciaStay
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            EntryDate = DateTime.UtcNow.AddDays(-10),
            ExitDate = null
        };
        db.CasaConvivenciaStays.Add(stay);

        await db.SaveChangesAsync();
        return (user.Id, person.Id);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenChangingToCompleted_UpdatesStatusAndLogsAudit()
    {
        using var db = CreateDbContext();
        var (actorId, personId) = await SeedDependenciesAsync(db);

        var visit = new CasonaVisit
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            VisitorName = "María Familiar",
            Date = DateTime.UtcNow.Date,
            StartTime = new TimeSpan(15, 0, 0),
            EstimatedDurationMinutes = 60,
            Status = VisitStatus.Pending,
            CreatedByUserId = actorId,
            CreatedAt = DateTime.UtcNow
        };
        db.CasonaVisits.Add(visit);
        await db.SaveChangesAsync();

        var service = new CasonaVisitService(db);

        var dto = new ChangeCasonaVisitStatusDto(VisitStatus.Completed, null);
        var result = await service.ChangeStatusAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.CasonaVisits.SingleAsync(v => v.Id == visit.Id);
        Assert.Equal(VisitStatus.Completed, updated.Status);
        Assert.Null(updated.CancellationReason);

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Visita marcada como realizada", log.Action);
        Assert.Equal($"CasonaVisit:{visit.Id}", log.AffectedEntity);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenChangingToCancelled_SavesReasonAndLogsAudit()
    {
        using var db = CreateDbContext();
        var (actorId, personId) = await SeedDependenciesAsync(db);

        var visit = new CasonaVisit
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            VisitorName = "Carlos Externo",
            Date = DateTime.UtcNow.Date,
            StartTime = new TimeSpan(10, 0, 0),
            EstimatedDurationMinutes = 45,
            Status = VisitStatus.Pending,
            CreatedByUserId = actorId,
            CreatedAt = DateTime.UtcNow
        };
        db.CasonaVisits.Add(visit);
        await db.SaveChangesAsync();

        var service = new CasonaVisitService(db);

        var dto = new ChangeCasonaVisitStatusDto(VisitStatus.Cancelled, "No pudo viajar por lluvia");
        var result = await service.ChangeStatusAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.CasonaVisits.SingleAsync(v => v.Id == visit.Id);
        Assert.Equal(VisitStatus.Cancelled, updated.Status);
        Assert.Equal("No pudo viajar por lluvia", updated.CancellationReason);

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Visita cancelada", log.Action);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenVisitNotFound_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var (actorId, _) = await SeedDependenciesAsync(db);
        var service = new CasonaVisitService(db);

        var dto = new ChangeCasonaVisitStatusDto(VisitStatus.Completed, null);
        var result = await service.ChangeStatusAsync(Guid.NewGuid(), dto, actorId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChangeCasonaVisitStatusError.NotFound, result.Error);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotWasOccupiedByCancelledVisit_DoesNotConflict()
    {
        using var db = CreateDbContext();
        var (actorId, personId) = await SeedDependenciesAsync(db);

        var visitDate = DateTime.UtcNow.Date.AddDays(1);

        var cancelledVisit = new CasonaVisit
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            VisitorName = "Familiar previo",
            Date = visitDate,
            StartTime = new TimeSpan(16, 0, 0),
            EstimatedDurationMinutes = 60,
            Status = VisitStatus.Cancelled,
            CancellationReason = "Cancelada con anticipación",
            CreatedByUserId = actorId,
            CreatedAt = DateTime.UtcNow
        };
        db.CasonaVisits.Add(cancelledVisit);
        await db.SaveChangesAsync();

        var service = new CasonaVisitService(db);

        var newDto = new CreateCasonaVisitDto(
            personId,
            "Nuevo visitante",
            visitDate,
            new TimeSpan(16, 15, 0),
            45);

        var result = await service.CreateAsync(newDto, actorId, CancellationToken.None);

        Assert.Null(result.Error);
        Assert.NotEqual(Guid.Empty, result.CasonaVisitId);
    }
}