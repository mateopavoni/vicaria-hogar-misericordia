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

        var result = await service.UpdateStageAsync(personId, LifeStoryStage.InCentroBarrial, "  En el hogar...  ", actorId);

        Assert.NotNull(result);
        var entity = await db.LifeStories.FirstAsync(l => l.PersonId == personId);
        Assert.Equal("En el hogar...", entity.InCentroBarrial);
        Assert.Equal(actorId, entity.InCentroBarrialUpdatedByUserId);
        Assert.True(entity.InCentroBarrialUpdatedAt.HasValue);
        Assert.Null(entity.BeforeCentroBarrial);
        Assert.Null(entity.AfterCentroBarrial);
        Assert.True(result!.InCentroBarrial.IsCompleted);
    }

    [Fact]
    public async Task UpdateStageAsync_EditaUnaEtapaSinTocarLasOtras()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = Guid.NewGuid();

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeCentroBarrial, "antes", actorId);
        await service.UpdateStageAsync(personId, LifeStoryStage.AfterCentroBarrial, "despues", actorId);

        var result = await service.UpdateStageAsync(personId, LifeStoryStage.InCentroBarrial, "en", actorId);

        Assert.NotNull(result);
        Assert.Equal("antes", result!.BeforeCentroBarrial.Content);
        Assert.Equal("en", result.InCentroBarrial.Content);
        Assert.Equal("despues", result.AfterCentroBarrial.Content);
    }

    [Fact]
    public async Task UpdateStageAsync_ConMismoContenido_NoReescribeAutorYTiempo()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = Guid.NewGuid();

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeCentroBarrial, "igual", actorId);

        var fixedOld = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = await db.LifeStories.FirstAsync(l => l.PersonId == personId);
        entity.BeforeCentroBarrialUpdatedAt = fixedOld;
        await db.SaveChangesAsync();

        var result = await service.UpdateStageAsync(personId, LifeStoryStage.BeforeCentroBarrial, "igual", actorId);

        entity = await db.LifeStories.FirstAsync(l => l.PersonId == personId);
        Assert.Equal(fixedOld, entity.BeforeCentroBarrialUpdatedAt);
        Assert.Equal("igual", result!.BeforeCentroBarrial.Content);
    }

    [Fact]
    public async Task UpdateStageAsync_PersonaInexistente_DevuelveNull()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);

        var result = await service.UpdateStageAsync(Guid.NewGuid(), LifeStoryStage.BeforeCentroBarrial, "x", Guid.NewGuid());

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
        Assert.False(result.BeforeCentroBarrial.IsCompleted);
        Assert.Null(result.BeforeCentroBarrial.Content);
        Assert.False(result.InCentroBarrial.IsCompleted);
        Assert.False(result.AfterCentroBarrial.IsCompleted);
        Assert.Empty(result.BeforeCentroBarrial.Entries);
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

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeCentroBarrial, "primera entrada", actorId);
        var result = await service.UpdateStageAsync(personId, LifeStoryStage.BeforeCentroBarrial, "segunda entrada", actorId);

        Assert.NotNull(result);
        Assert.Equal(2, result!.BeforeCentroBarrial.Entries.Count);
        // orden descendente: la más nueva primero
        Assert.Equal("segunda entrada", result.BeforeCentroBarrial.Entries[0].Content);
        Assert.Equal("primera entrada", result.BeforeCentroBarrial.Entries[1].Content);
        // el "valor actual" (compatibilidad con quien solo lea Content) refleja la última
        Assert.Equal("segunda entrada", result.BeforeCentroBarrial.Content);
    }

    [Fact]
    public async Task UpdateStageAsync_ConContenidoVacio_NoSumaEntradaAlHistorial()
    {
        using var db = CrearDbContext();
        var service = new LifeStoryService(db);
        var personId = await CrearPersonaAsync(db);
        var actorId = Guid.NewGuid();

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeCentroBarrial, "   ", actorId);

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

        await service.UpdateStageAsync(personId, LifeStoryStage.BeforeCentroBarrial, "antes", actorId);
        await service.UpdateStageAsync(personId, LifeStoryStage.InCentroBarrial, "en", actorId);

        var result = await service.GetByPersonIdAsync(personId);

        Assert.Single(result.BeforeCentroBarrial.Entries);
        Assert.Single(result.InCentroBarrial.Entries);
        Assert.Empty(result.AfterCentroBarrial.Entries);
    }
}