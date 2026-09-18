using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CasonaStays;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CasonaStays;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CasonaStays;

public class CasonaStayServiceTests
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

    private async Task<CasonaStay> CrearEstadiaActiva(VicariaDbContext db, Guid? personId = null)
    {
        var person = new Person { Id = personId ?? Guid.NewGuid(), FirstName = "Ana", CreatedAt = DateTime.UtcNow };
        db.People.Add(person);

        var stay = new CasonaStay
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            EntryDate = DateTime.UtcNow.AddDays(-10)
        };
        db.CasonaStays.Add(stay);
        await db.SaveChangesAsync();
        return stay;
    }

    [Fact]
    public async Task ExitAsync_ConEstadiaActiva_RegistraEgresoConFechaAutomatica()
    {
        var beforeExit = DateTime.UtcNow;
        using var db = CrearDbContext();
        var service = new CasonaStayService(db);
        var stay = await CrearEstadiaActiva(db);

        var resultado = await service.ExitAsync(stay.Id, new CasonaStayExitDto(null, null), Guid.NewGuid());

        Assert.True(resultado.Success);
        var egresada = await db.CasonaStays.FindAsync(stay.Id);
        Assert.NotNull(egresada!.ExitDate);
        Assert.True(egresada.ExitDate >= beforeExit);
    }

    [Fact]
    public async Task ExitAsync_ConMotivoOtroYTexto_GuardaAmbos()
    {
        using var db = CrearDbContext();
        var service = new CasonaStayService(db);
        var stay = await CrearEstadiaActiva(db);

        var resultado = await service.ExitAsync(stay.Id, new CasonaStayExitDto(StayExitReason.Other, "Se retiró por motivos personales"), Guid.NewGuid());

        Assert.True(resultado.Success);
        var egresada = await db.CasonaStays.FindAsync(stay.Id);
        Assert.Equal(StayExitReason.Other, egresada!.ExitReason);
        Assert.Equal("Se retiró por motivos personales", egresada.Reason);
    }

    [Fact]
    public async Task ExitAsync_ConMotivoEspecificoYTexto_GuardaExitReasonYTambienTexto()
    {
        using var db = CrearDbContext();
        var service = new CasonaStayService(db);
        var stay = await CrearEstadiaActiva(db);

        var resultado = await service.ExitAsync(stay.Id, new CasonaStayExitDto(StayExitReason.Abandonment, "No volvió"), Guid.NewGuid());

        Assert.True(resultado.Success);
        var egresada = await db.CasonaStays.FindAsync(stay.Id);
        Assert.Equal(StayExitReason.Abandonment, egresada!.ExitReason);
        Assert.Equal("No volvió", egresada.Reason);
    }

    [Fact]
    public async Task ExitAsync_SinMotivo_GuardaNullEnExitReasonYReason()
    {
        using var db = CrearDbContext();
        var service = new CasonaStayService(db);
        var stay = await CrearEstadiaActiva(db);

        var resultado = await service.ExitAsync(stay.Id, new CasonaStayExitDto(null, null), Guid.NewGuid());

        Assert.True(resultado.Success);
        var egresada = await db.CasonaStays.FindAsync(stay.Id);
        Assert.Null(egresada!.ExitReason);
        Assert.Null(egresada.Reason);
    }

    [Fact]
    public async Task ExitAsync_ConEstadiaInexistente_DevuelveStayNotFound()
    {
        using var db = CrearDbContext();
        var service = new CasonaStayService(db);

        var resultado = await service.ExitAsync(Guid.NewGuid(), new CasonaStayExitDto(null, null), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(CasonaStayExitError.StayNotFound, resultado.Error);
    }

    [Fact]
    public async Task ExitAsync_ConEstadiaYaEgresada_DevuelveAlreadyExited()
    {
        using var db = CrearDbContext();
        var service = new CasonaStayService(db);
        var stay = await CrearEstadiaActiva(db);
        stay.ExitDate = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var resultado = await service.ExitAsync(stay.Id, new CasonaStayExitDto(StayExitReason.TeamDischarge, null), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(CasonaStayExitError.AlreadyExited, resultado.Error);
    }

    [Fact]
    public async Task ExitAsync_RegistraAuditLogConElActor()
    {
        using var db = CrearDbContext();
        var service = new CasonaStayService(db);
        var stay = await CrearEstadiaActiva(db);
        var actorId = Guid.NewGuid();

        await service.ExitAsync(stay.Id, new CasonaStayExitDto(StayExitReason.VoluntaryDischarge, null), actorId);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.AffectedEntity == $"CasonaStay:{stay.Id}" && a.UserId == actorId);
        Assert.NotNull(log);
    }
}