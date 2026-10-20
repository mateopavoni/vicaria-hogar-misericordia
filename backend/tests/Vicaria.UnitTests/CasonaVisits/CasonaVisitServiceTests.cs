using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CasonaVisits;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CasonaVisits;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CasonaVisits;

public class CasonaVisitServiceTests
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

    private static async Task<Guid> SeedUserAsync(VicariaDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Actor",
            LastName = "Test",
            Email = $"{Guid.NewGuid()}@mail.com",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Guid> SeedPersonAsync(
        VicariaDbContext db,
        Guid createdByUserId,
        PersonType personType = PersonType.Resident,
        bool openStay = true)
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Carla",
            LastName = "Residente",
            CreatedAt = DateTime.UtcNow
        };
        db.People.Add(person);

        db.SocialRecords.Add(new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            PersonType = personType,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.CasaConvivenciaStays.Add(new CasaConvivenciaStay
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            EntryDate = DateTime.UtcNow.AddDays(-10),
            ExitDate = openStay ? null : DateTime.UtcNow.AddDays(-1)
        });

        await db.SaveChangesAsync();
        return person.Id;
    }

    private static async Task<CasonaVisit> SeedVisitAsync(
        VicariaDbContext db,
        Guid personId,
        Guid createdByUserId,
        DateTime? date = null,
        TimeSpan? startTime = null,
        int durationMinutes = 60,
        VisitStatus status = VisitStatus.Pending)
    {
        var visit = new CasonaVisit
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            VisitorName = "Visitante Seed",
            Date = (date ?? DateTime.UtcNow.Date.AddDays(1)).Date,
            StartTime = startTime ?? new TimeSpan(16, 0, 0),
            EstimatedDurationMinutes = durationMinutes,
            Status = status,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
        db.CasonaVisits.Add(visit);
        await db.SaveChangesAsync();
        return visit;
    }

    private static DateTime Tomorrow => DateTime.UtcNow.Date.AddDays(1);

    private static CreateCasonaVisitDto NewVisitDto(
        Guid personId,
        DateTime? date = null,
        TimeSpan? startTime = null,
        int durationMinutes = 60) =>
        new(personId, "  María Visitante  ", date ?? Tomorrow, startTime ?? new TimeSpan(16, 0, 0), durationMinutes);

    [Fact]
    public async Task CreateAsync_WithValidDto_PersistsVisitPendingWithActorAndTrimmedName()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var service = new CasonaVisitService(db);

        var result = await service.CreateAsync(NewVisitDto(personId), actorId, CancellationToken.None);

        Assert.True(result.Success);
        var stored = db.CasonaVisits.Single(v => v.Id == result.CasonaVisitId);
        Assert.Equal(personId, stored.PersonId);
        Assert.Equal("María Visitante", stored.VisitorName);
        Assert.Equal(Tomorrow, stored.Date);
        Assert.Equal(new TimeSpan(16, 0, 0), stored.StartTime);
        Assert.Equal(60, stored.EstimatedDurationMinutes);
        Assert.Equal(VisitStatus.Pending, stored.Status);
        Assert.Equal(actorId, stored.CreatedByUserId);
        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_WritesAuditLogEntry()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var service = new CasonaVisitService(db);

        var result = await service.CreateAsync(NewVisitDto(personId), actorId, CancellationToken.None);

        var log = db.AuditLogs.Single();
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Visita creada", log.Action);
        Assert.Equal($"CasonaVisit:{result.CasonaVisitId}", log.AffectedEntity);
    }

    [Fact]
    public async Task CreateAsync_UnknownPerson_ReturnsPersonNotFound()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new CasonaVisitService(db);

        var result = await service.CreateAsync(NewVisitDto(Guid.NewGuid()), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateCasonaVisitError.PersonNotFound, result.Error);
        Assert.Empty(db.CasonaVisits);
    }

    [Fact]
    public async Task CreateAsync_PersonNotResident_ReturnsPersonNotResident()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId, personType: PersonType.Ambulatory);
        var service = new CasonaVisitService(db);

        var result = await service.CreateAsync(NewVisitDto(personId), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateCasonaVisitError.PersonNotResident, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithoutOpenStay_ReturnsNoOpenStay()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId, openStay: false);
        var service = new CasonaVisitService(db);

        var result = await service.CreateAsync(NewVisitDto(personId), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateCasonaVisitError.NoOpenStay, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithOverlappingVisit_ReturnsTimeOverlap()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        await SeedVisitAsync(db, personId, actorId, startTime: new TimeSpan(16, 0, 0), durationMinutes: 60);
        var service = new CasonaVisitService(db);

        // 16:30 cae dentro de 16:00-17:00
        var result = await service.CreateAsync(
            NewVisitDto(personId, startTime: new TimeSpan(16, 30, 0)),
            actorId,
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateCasonaVisitError.TimeOverlap, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithAdjacentVisit_Ok()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        await SeedVisitAsync(db, personId, actorId, startTime: new TimeSpan(16, 0, 0), durationMinutes: 60);
        var service = new CasonaVisitService(db);

        // termina exactamente cuando empieza la otra: no se pisa
        var result = await service.CreateAsync(
            NewVisitDto(personId, startTime: new TimeSpan(17, 0, 0)),
            actorId,
            CancellationToken.None);

        Assert.True(result.Success);
    }

    [Theory]
    [InlineData(VisitStatus.Completed)]
    [InlineData(VisitStatus.Cancelled)]
    public async Task UpdateAsync_OnNonPendingVisit_ReturnsInvalidState(VisitStatus status)
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var visit = await SeedVisitAsync(db, personId, actorId, status: status);
        var service = new CasonaVisitService(db);

        var dto = new UpdateCasonaVisitDto(
            personId, "Otro Visitante", Tomorrow, new TimeSpan(16, 0, 0), 60, VisitStatus.Pending, null);

        var result = await service.UpdateAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(UpdateCasonaVisitError.InvalidState, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_UnknownVisit_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var service = new CasonaVisitService(db);

        var dto = new UpdateCasonaVisitDto(
            personId, "Visitante", Tomorrow, new TimeSpan(16, 0, 0), 60, VisitStatus.Pending, null);

        var result = await service.UpdateAsync(Guid.NewGuid(), dto, actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(UpdateCasonaVisitError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_OnPendingVisit_PersistsChangesAndWritesAuditLog()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var visit = await SeedVisitAsync(db, personId, actorId);
        var service = new CasonaVisitService(db);

        var dto = new UpdateCasonaVisitDto(
            personId, "Nuevo Visitante", Tomorrow, new TimeSpan(18, 0, 0), 90, VisitStatus.Pending, null);

        var result = await service.UpdateAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.Success);
        var stored = db.CasonaVisits.Single(v => v.Id == visit.Id);
        Assert.Equal("Nuevo Visitante", stored.VisitorName);
        Assert.Equal(new TimeSpan(18, 0, 0), stored.StartTime);
        Assert.Equal(90, stored.EstimatedDurationMinutes);
        Assert.Equal(VisitStatus.Pending, stored.Status);

        var log = db.AuditLogs.Single();
        Assert.Equal("Visita actualizada", log.Action);
        Assert.Equal($"CasonaVisit:{visit.Id}", log.AffectedEntity);
        Assert.Equal(actorId, log.UserId);
    }

    [Fact]
    public async Task UpdateAsync_MarkingCompletedAPastVisit_Ok()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        // visita de ayer todavía Pending: la regla de pasado no aplica si fecha/hora no cambian
        var visit = await SeedVisitAsync(db, personId, actorId, date: DateTime.UtcNow.Date.AddDays(-1));
        var service = new CasonaVisitService(db);

        var dto = new UpdateCasonaVisitDto(
            personId, visit.VisitorName, visit.Date, visit.StartTime, visit.EstimatedDurationMinutes,
            VisitStatus.Completed, null);

        var result = await service.UpdateAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(VisitStatus.Completed, db.CasonaVisits.Single(v => v.Id == visit.Id).Status);
    }

    [Fact]
    public async Task UpdateAsync_MovingVisitToPast_ReturnsVisitInPast()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var visit = await SeedVisitAsync(db, personId, actorId);
        var service = new CasonaVisitService(db);

        var dto = new UpdateCasonaVisitDto(
            personId, visit.VisitorName, DateTime.UtcNow.Date.AddDays(-3), visit.StartTime,
            visit.EstimatedDurationMinutes, VisitStatus.Pending, null);

        var result = await service.UpdateAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(UpdateCasonaVisitError.VisitInPast, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_ChangingStatusToCancelled_PersistsReason()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var visit = await SeedVisitAsync(db, personId, actorId);
        var service = new CasonaVisitService(db);

        var dto = new UpdateCasonaVisitDto(
            personId, visit.VisitorName, visit.Date, visit.StartTime, visit.EstimatedDurationMinutes,
            VisitStatus.Cancelled, "  El visitante no puede venir  ");

        var result = await service.UpdateAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.Success);
        var stored = db.CasonaVisits.Single(v => v.Id == visit.Id);
        Assert.Equal(VisitStatus.Cancelled, stored.Status);
        Assert.Equal("El visitante no puede venir", stored.CancellationReason);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnSchedule_DoesNotOverlapWithItself()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var visit = await SeedVisitAsync(db, personId, actorId);
        var service = new CasonaVisitService(db);

        // misma fecha y hora que la visita que se edita: no debe chocar consigo misma
        var dto = new UpdateCasonaVisitDto(
            personId, "Otro Nombre", visit.Date, visit.StartTime, visit.EstimatedDurationMinutes,
            VisitStatus.Pending, null);

        var result = await service.UpdateAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateAsync_OverlappingAnotherVisit_ReturnsTimeOverlap()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var visit = await SeedVisitAsync(db, personId, actorId, startTime: new TimeSpan(16, 0, 0), durationMinutes: 60);
        await SeedVisitAsync(db, personId, actorId, startTime: new TimeSpan(18, 0, 0), durationMinutes: 60);
        var service = new CasonaVisitService(db);

        var dto = new UpdateCasonaVisitDto(
            personId, visit.VisitorName, visit.Date, new TimeSpan(18, 30, 0), 60, VisitStatus.Pending, null);

        var result = await service.UpdateAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(UpdateCasonaVisitError.TimeOverlap, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_PersonNotResident_ReturnsPersonNotResident()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var visit = await SeedVisitAsync(db, personId, actorId);
        var ambulatoryId = await SeedPersonAsync(db, actorId, personType: PersonType.Ambulatory);
        var service = new CasonaVisitService(db);

        var dto = new UpdateCasonaVisitDto(
            ambulatoryId, visit.VisitorName, visit.Date, visit.StartTime, visit.EstimatedDurationMinutes,
            VisitStatus.Pending, null);

        var result = await service.UpdateAsync(visit.Id, dto, actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(UpdateCasonaVisitError.PersonNotResident, result.Error);
    }

    [Fact]
    public async Task GetByRangeAsync_FiltersOrdersAndPaginates()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);

        // 12 visitas el mismo día (seed directo, sin pasar por el servicio) para forzar 2 páginas
        for (var i = 0; i < 12; i++)
        {
            await SeedVisitAsync(
                db, personId, actorId,
                date: Tomorrow,
                startTime: new TimeSpan(8 + i, 0, 0),
                durationMinutes: 30);
        }
        // una fuera del rango que no debe aparecer
        await SeedVisitAsync(db, personId, actorId, date: Tomorrow.AddDays(30));

        var service = new CasonaVisitService(db);

        var page1 = await service.GetByRangeAsync(Tomorrow, Tomorrow, 1, CancellationToken.None);

        Assert.Equal(12, page1.Total);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(new TimeSpan(8, 0, 0), page1.Items[0].StartTime);
        Assert.Equal("Carla Residente", page1.Items[0].PersonName);

        var page2 = await service.GetByRangeAsync(Tomorrow, Tomorrow, 2, CancellationToken.None);

        Assert.Equal(12, page2.Total);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(new TimeSpan(18, 0, 0), page2.Items[0].StartTime);
    }

    [Fact]
    public async Task GetByRangeAsync_EmptyRange_ReturnsEmptyPagedResult()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new CasonaVisitService(db);

        var result = await service.GetByRangeAsync(Tomorrow, Tomorrow, 1, CancellationToken.None);

        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Items);
    }
}
