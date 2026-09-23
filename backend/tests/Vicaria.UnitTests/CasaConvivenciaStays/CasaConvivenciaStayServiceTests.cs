using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CasaConvivenciaStays;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CasaConvivenciaStays;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CasaConvivenciaStays;

public class CasaConvivenciaStayServiceTests
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

    private async Task<CasaConvivenciaStay> CrearEstadiaActiva(VicariaDbContext db, Guid? personId = null)
    {
        var person = new Person { Id = personId ?? Guid.NewGuid(), FirstName = "Ana", CreatedAt = DateTime.UtcNow };
        db.People.Add(person);

        var stay = new CasaConvivenciaStay
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            EntryDate = DateTime.UtcNow.AddDays(-10)
        };
        db.CasaConvivenciaStays.Add(stay);
        await db.SaveChangesAsync();
        return stay;
    }

    [Fact]
    public async Task ExitAsync_ConEstadiaActiva_RegistraEgresoConFechaAutomatica()
    {
        var beforeExit = DateTime.UtcNow;
        using var db = CrearDbContext();
        var service = new CasaConvivenciaStayService(db);
        var stay = await CrearEstadiaActiva(db);

        var resultado = await service.ExitAsync(stay.Id, new CasaConvivenciaStayExitDto(null, null), Guid.NewGuid());

        Assert.True(resultado.Success);
        var egresada = await db.CasaConvivenciaStays.FindAsync(stay.Id);
        Assert.NotNull(egresada!.ExitDate);
        Assert.True(egresada.ExitDate >= beforeExit);
    }

    [Fact]
    public async Task ExitAsync_ConMotivoOtroYTexto_GuardaAmbos()
    {
        using var db = CrearDbContext();
        var service = new CasaConvivenciaStayService(db);
        var stay = await CrearEstadiaActiva(db);

        var resultado = await service.ExitAsync(stay.Id, new CasaConvivenciaStayExitDto(StayExitReason.Other, "Se retiró por motivos personales"), Guid.NewGuid());

        Assert.True(resultado.Success);
        var egresada = await db.CasaConvivenciaStays.FindAsync(stay.Id);
        Assert.Equal(StayExitReason.Other, egresada!.ExitReason);
        Assert.Equal("Se retiró por motivos personales", egresada.Reason);
    }

    [Fact]
    public async Task ExitAsync_ConMotivoEspecificoYTexto_GuardaExitReasonYTambienTexto()
    {
        using var db = CrearDbContext();
        var service = new CasaConvivenciaStayService(db);
        var stay = await CrearEstadiaActiva(db);

        var resultado = await service.ExitAsync(stay.Id, new CasaConvivenciaStayExitDto(StayExitReason.Abandonment, "No volvió"), Guid.NewGuid());

        Assert.True(resultado.Success);
        var egresada = await db.CasaConvivenciaStays.FindAsync(stay.Id);
        Assert.Equal(StayExitReason.Abandonment, egresada!.ExitReason);
        Assert.Equal("No volvió", egresada.Reason);
    }

    [Fact]
    public async Task ExitAsync_SinMotivo_GuardaNullEnExitReasonYReason()
    {
        using var db = CrearDbContext();
        var service = new CasaConvivenciaStayService(db);
        var stay = await CrearEstadiaActiva(db);

        var resultado = await service.ExitAsync(stay.Id, new CasaConvivenciaStayExitDto(null, null), Guid.NewGuid());

        Assert.True(resultado.Success);
        var egresada = await db.CasaConvivenciaStays.FindAsync(stay.Id);
        Assert.Null(egresada!.ExitReason);
        Assert.Null(egresada.Reason);
    }

    [Fact]
    public async Task ExitAsync_ConEstadiaInexistente_DevuelveStayNotFound()
    {
        using var db = CrearDbContext();
        var service = new CasaConvivenciaStayService(db);

        var resultado = await service.ExitAsync(Guid.NewGuid(), new CasaConvivenciaStayExitDto(null, null), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(CasaConvivenciaStayExitError.StayNotFound, resultado.Error);
    }

    [Fact]
    public async Task ExitAsync_ConEstadiaYaEgresada_DevuelveAlreadyExited()
    {
        using var db = CrearDbContext();
        var service = new CasaConvivenciaStayService(db);
        var stay = await CrearEstadiaActiva(db);
        stay.ExitDate = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var resultado = await service.ExitAsync(stay.Id, new CasaConvivenciaStayExitDto(StayExitReason.TeamDischarge, null), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(CasaConvivenciaStayExitError.AlreadyExited, resultado.Error);
    }

    [Fact]
    public async Task ExitAsync_RegistraAuditLogConElActor()
    {
        using var db = CrearDbContext();
        var service = new CasaConvivenciaStayService(db);
        var stay = await CrearEstadiaActiva(db);
        var actorId = Guid.NewGuid();

        await service.ExitAsync(stay.Id, new CasaConvivenciaStayExitDto(StayExitReason.VoluntaryDischarge, null), actorId);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.AffectedEntity == $"CasaConvivenciaStay:{stay.Id}" && a.UserId == actorId);
        Assert.NotNull(log);
    }
}