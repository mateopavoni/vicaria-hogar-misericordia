using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Attendances;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Attendances;

public class AttendanceServiceTests
{
    private static VicariaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new VicariaDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<Guid> CrearPersonaConFichaActiva(VicariaDbContext db, DateTime? createdAt = null)
    {
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Ana", CreatedAt = createdAt ?? DateTime.UtcNow };
        db.People.Add(person);

        var record = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            PersonType = PersonType.Ambulatory,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = createdAt ?? DateTime.UtcNow
        };
        db.SocialRecords.Add(record);

        await db.SaveChangesAsync();
        return person.Id;
    }

    private static async Task SeedAttendance(VicariaDbContext db, Guid personId, DateTime date)
    {
        db.Attendances.Add(new Attendance
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Date = date,
            CreatedByUserId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task RegisterAsync_PersonaActiva_RegistraAsistenciaYNoCambiaEstado()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaConFichaActiva(db);
        var service = new AttendanceService(db);

        var resultado = await service.RegisterAsync(new CreateAttendanceDto(personId), Guid.NewGuid());

        Assert.True(resultado.Success);
        Assert.Single(await db.Attendances.Where(a => a.PersonId == personId).ToListAsync());
        var record = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Active, record.Status);
    }

    [Fact]
    public async Task RegisterAsync_PersonaInactiva_ReactivaYRegistraAuditLog()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaConFichaActiva(db);
        var record = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        record.Status = SocialRecordStatus.Inactive;
        await db.SaveChangesAsync();

        var actorId = Guid.NewGuid();
        var service = new AttendanceService(db);

        var resultado = await service.RegisterAsync(new CreateAttendanceDto(personId), actorId);

        Assert.True(resultado.Success);
        var reactivada = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Active, reactivada.Status);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a =>
            a.AffectedEntity == $"SocialRecord:{record.Id}" && a.UserId == actorId);
        Assert.NotNull(log);
    }

    [Fact]
    public async Task RegisterAsync_PersonaInexistente_DevuelvePersonNotFound()
    {
        using var db = CrearDbContext();
        var service = new AttendanceService(db);

        var resultado = await service.RegisterAsync(new CreateAttendanceDto(Guid.NewGuid()), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(RegisterAttendanceError.PersonNotFound, resultado.Error);
    }

    [Fact]
    public async Task RegisterAsync_PersonaSinFicha_DevuelveSocialRecordNotFound()
    {
        using var db = CrearDbContext();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "SinFicha", CreatedAt = DateTime.UtcNow };
        db.People.Add(person);
        await db.SaveChangesAsync();
        var service = new AttendanceService(db);

        var resultado = await service.RegisterAsync(new CreateAttendanceDto(person.Id), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(RegisterAttendanceError.SocialRecordNotFound, resultado.Error);
    }

    [Fact]
    public async Task CheckInactivity_AsistenciaReciente_MantieneActivo()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaConFichaActiva(db);
        await SeedAttendance(db, personId, DateTime.UtcNow.AddDays(-5));
        var service = new AttendanceInactivityService(db);

        await service.CheckAndProcessInactivityAsync();

        var record = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Active, record.Status);
    }

    [Fact]
    public async Task CheckInactivity_AsistenciaVieja_PasaAInactivoConAuditLog()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaConFichaActiva(db);
        var record = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        await SeedAttendance(db, personId, DateTime.UtcNow.AddDays(-31));
        var service = new AttendanceInactivityService(db);

        await service.CheckAndProcessInactivityAsync();

        var inactiva = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Inactive, inactiva.Status);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.AffectedEntity == $"SocialRecord:{record.Id}");
        Assert.NotNull(log);
    }

    [Fact]
    public async Task CheckInactivity_SinNingunaAsistencia_PasaAInactivo()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaConFichaActiva(db);
        var service = new AttendanceInactivityService(db);

        await service.CheckAndProcessInactivityAsync();

        var record = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Inactive, record.Status);
    }

    [Fact]
    public async Task CheckInactivity_SoloInactivos_NoTocaNada()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaConFichaActiva(db);
        var record = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        record.Status = SocialRecordStatus.Inactive;
        await db.SaveChangesAsync();
        var service = new AttendanceInactivityService(db);

        await service.CheckAndProcessInactivityAsync();

        var stillInactive = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Inactive, stillInactive.Status);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }
}