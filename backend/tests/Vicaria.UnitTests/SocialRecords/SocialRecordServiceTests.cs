using Microsoft.EntityFrameworkCore;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.SocialRecords;

namespace Vicaria.UnitTests.SocialRecords;

public class SocialRecordServiceTests
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

    [Fact]
    public async Task CreateAsync_ConSoloElNombre_CreaPersonaYFichaActiva()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null, null);

        var resultado = await service.CreateAsync(dto, Guid.NewGuid());

        Assert.True(resultado.Success);
        var persona = await db.People.FindAsync(resultado.PersonId);
        Assert.Equal("Ana", persona!.FirstName);
        var ficha = await db.SocialRecords.FindAsync(resultado.SocialRecordId);
        Assert.Equal(SocialRecordStatus.Active, ficha!.Status);
    }

    [Fact]
    public async Task CreateAsync_ConContacto_LoVinculaALaFicha()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var contacto = new ContactDto("Juan", "Perez", "1234", "Calle Falsa 123");
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null, contacto);

        var resultado = await service.CreateAsync(dto, Guid.NewGuid());

        var contactoGuardado = await db.Contacts.FirstOrDefaultAsync(c => c.SocialRecordId == resultado.SocialRecordId);
        Assert.NotNull(contactoGuardado);
        Assert.Equal("Juan", contactoGuardado!.FirstName);
    }

    [Fact]
    public async Task CreateAsync_SinContacto_NoCreaContacto()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null, null);

        var resultado = await service.CreateAsync(dto, Guid.NewGuid());

        var hayContacto = await db.Contacts.AnyAsync(c => c.SocialRecordId == resultado.SocialRecordId);
        Assert.False(hayContacto);
    }

    [Fact]
    public async Task CreateAsync_RegistraAuditLogConElActor()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var actorId = Guid.NewGuid();
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null, null);

        var resultado = await service.CreateAsync(dto, actorId);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.AffectedEntity == $"SocialRecord:{resultado.SocialRecordId}");
        Assert.NotNull(log);
        Assert.Equal(actorId, log!.UserId);
    }

    [Fact]
    public async Task SearchAsync_ConTextoParcial_EncuentraLaFicha()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ramón", "Gómez", null, null, null, null, null, null, null, null, false, null, null);
        await service.CreateAsync(dto, Guid.NewGuid());

        var resultados = await service.SearchAsync("gom");

        Assert.Single(resultados);
        Assert.Equal("Ramón Gómez", resultados[0].FullName);
    }

    [Fact]
    public async Task SearchAsync_IgnoraTildesYMayusculas()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ramón", "Gómez", null, null, null, null, null, null, null, null, false, null, null);
        await service.CreateAsync(dto, Guid.NewGuid());

        var resultados = await service.SearchAsync("RAMON");

        Assert.Single(resultados);
    }

    [Fact]
    public async Task SearchAsync_SinCoincidencias_DevuelveListaVacia()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null, null);
        await service.CreateAsync(dto, Guid.NewGuid());

        var resultados = await service.SearchAsync("noexiste");

        Assert.Empty(resultados);
    }

    [Fact]
    public async Task SearchAsync_ConQueryVacio_DevuelveListaVacia()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);

        var resultados = await service.SearchAsync("");

        Assert.Empty(resultados);
    }

    [Fact]
    public async Task UpdateAsync_ConFichaExistente_ActualizaLosDatos()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null, null), Guid.NewGuid());
        var dto = new UpdateSocialRecordDto("Ana", "Torres", "30111222", null, null, "Nuevo motivo", null, null, null, null, true, null);

        var resultado = await service.UpdateAsync(creada.SocialRecordId, dto, Guid.NewGuid());

        Assert.True(resultado.Success);
        var persona = await db.People.FindAsync(creada.PersonId);
        Assert.Equal("Torres", persona!.LastName);
        var ficha = await db.SocialRecords.FindAsync(creada.SocialRecordId);
        Assert.Equal("Nuevo motivo", ficha!.ReasonForEntry);
    }

    [Fact]
    public async Task UpdateAsync_ConFichaInexistente_DevuelveNotFound()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null);

        var resultado = await service.UpdateAsync(Guid.NewGuid(), dto, Guid.NewGuid());

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task UpdateAsync_RegistraAuditLogConElActor()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null, null), Guid.NewGuid());
        var actorId = Guid.NewGuid();
        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, false, null);

        await service.UpdateAsync(creada.SocialRecordId, dto, actorId);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.AffectedEntity == $"SocialRecord:{creada.SocialRecordId}" && a.UserId == actorId);
        Assert.NotNull(log);
    }
}
