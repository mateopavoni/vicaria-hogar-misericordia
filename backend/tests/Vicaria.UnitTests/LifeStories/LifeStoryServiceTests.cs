using Microsoft.EntityFrameworkCore;
using Vicaria.Application.LifeStories;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.LifeStories;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.LifeStories;

public class LifeStoryServiceTests
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

    private static async Task<Guid> CrearPersonaAsync(VicariaDbContext db)
    {
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Ana" };
        db.People.Add(person);
        await db.SaveChangesAsync();
        return person.Id;
    }

    // el mapeo del historial de entradas hace Include(CreatedByUser); sin un User real
    // sembrado, el Include no resuelve y la entrada queda afuera del resultado.
    private static async Task<Guid> CrearUsuarioAsync(VicariaDbContext db)
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Autor", LastName = "De Prueba", Email = $"{Guid.NewGuid()}@test.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task UpdateStageAsync_PersonaExistenteSinHistoria_CreaFilaYEditaSoloLaEtapa()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = Guid.NewGuid();

        var result = await service.UpdateStageAsync(personId, LifeStoryStage.InHogar, "  En el hogar...  ", actorId);

        Assert.NotNull(result);
        var entity = await db.LifeStories.FirstAsync(l => l.PersonId == personId);
        Assert.Equal("En el hogar...", entity.InHogar);
        Assert.Equal(actorId, entity.InHogarUpdatedByUserId);
        Assert.True(entity.InHogarUpdatedAt.HasValue);
        Assert.Null(entity.BeforeHogar);
        Assert.Null(entity.AfterHogar);
        Assert.True(result!.InHogar.IsCompleted);
    }

    [Fact]
    public async Task UpdateStageAsync_EditaUnaEtapaSinTocarLasOtras()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = Guid.NewGuid();

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeHogar, "antes", actorId);
        await service.UpdateStageAsync(personId, LifeStoryStage.AfterHogar, "despues", actorId);

        var result = await service.UpdateStageAsync(personId, LifeStoryStage.InHogar, "en", actorId);

        Assert.NotNull(result);
        Assert.Equal("antes", result!.BeforeHogar.Content);
        Assert.Equal("en", result.InHogar.Content);
        Assert.Equal("despues", result.AfterHogar.Content);
    }

    [Fact]
    public async Task UpdateStageAsync_ConMismoContenido_NoReescribeAutorYTiempo()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = Guid.NewGuid();

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeHogar, "igual", actorId);

        var fixedOld = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = await db.LifeStories.FirstAsync(l => l.PersonId == personId);
        entity.BeforeHogarUpdatedAt = fixedOld;
        await db.SaveChangesAsync();

        var result = await service.UpdateStageAsync(personId, LifeStoryStage.BeforeHogar, "igual", actorId);

        entity = await db.LifeStories.FirstAsync(l => l.PersonId == personId);
        Assert.Equal(fixedOld, entity.BeforeHogarUpdatedAt);
        Assert.Equal("igual", result!.BeforeHogar.Content);
    }

    [Fact]
    public async Task UpdateStageAsync_PersonaInexistente_DevuelveNull()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);

        var result = await service.UpdateStageAsync(Guid.NewGuid(), LifeStoryStage.BeforeHogar, "x", Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_SinHistoria_DevuelveSeccionesVacias()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);

        var result = await service.GetByPersonIdAsync(personId);

        Assert.Equal(personId, result.PersonId);
        Assert.False(result.BeforeHogar.IsCompleted);
        Assert.Null(result.BeforeHogar.Content);
        Assert.False(result.InHogar.IsCompleted);
        Assert.False(result.AfterHogar.IsCompleted);
        Assert.Empty(result.BeforeHogar.Entries);
    }

    // bug reportado 2026-09-23: guardar una etapa pisaba el contenido anterior en vez de
    // sumar una entrada nueva al historial.
    [Fact]
    public async Task UpdateStageAsync_GuardaDosVeces_SumaDosEntradasAlHistorialSinPerderLaPrimera()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = await CrearUsuarioAsync(db);

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeHogar, "primera entrada", actorId);
        var result = await service.UpdateStageAsync(personId, LifeStoryStage.BeforeHogar, "segunda entrada", actorId);

        Assert.NotNull(result);
        Assert.Equal(2, result!.BeforeHogar.Entries.Count);
        // orden descendente: la más nueva primero
        Assert.Equal("segunda entrada", result.BeforeHogar.Entries[0].Content);
        Assert.Equal("primera entrada", result.BeforeHogar.Entries[1].Content);
        // el "valor actual" (compatibilidad con quien solo lea Content) refleja la última
        Assert.Equal("segunda entrada", result.BeforeHogar.Content);
    }

    [Fact]
    public async Task UpdateStageAsync_ConContenidoVacio_NoSumaEntradaAlHistorial()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = Guid.NewGuid();

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeHogar, "   ", actorId);

        var entries = await db.LifeStoryEntries.Where(e => e.PersonId == personId).ToListAsync();
        Assert.Empty(entries);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ConEntradasDeDistintasEtapas_LasSeparaCorrectamente()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = await CrearUsuarioAsync(db);

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeHogar, "antes", actorId);
        await service.UpdateStageAsync(personId, LifeStoryStage.InHogar, "en", actorId);

        var result = await service.GetByPersonIdAsync(personId);

        Assert.Single(result.BeforeHogar.Entries);
        Assert.Single(result.InHogar.Entries);
        Assert.Empty(result.AfterHogar.Entries);
    }
}