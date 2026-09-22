using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Observations;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Observations;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Observations;

public class ObservationCategoryServiceTests
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

    private static async Task<ObservationCategory> CrearCategoriaAsync(VicariaDbContext db, string nombre = "Salud", string? descripcion = null)
    {
        var categoria = new ObservationCategory
        {
            Id = Guid.NewGuid(),
            Name = nombre,
            Description = descripcion,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.ObservationCategories.Add(categoria);
        await db.SaveChangesAsync();
        return categoria;
    }

    [Fact]
    public async Task CreateAsync_ConDatosValidos_GuardaCategoriaYDescripcion()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);

        var resultado = await service.CreateAsync(new CreateObservationCategoryDto("Vinculación", "Acompañamiento de vínculos"), Guid.NewGuid());

        Assert.True(resultado.Success);
        Assert.NotNull(resultado.Data);
        Assert.Equal("Acompañamiento de vínculos", resultado.Data!.Description);
        var guardada = await db.ObservationCategories.SingleAsync(c => c.Name == "Vinculación");
        Assert.Equal("Acompañamiento de vínculos", guardada.Description);
        Assert.True(guardada.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ConDescripcionEspaciada_GuardaDescripcionTrimmeada()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);

        var resultado = await service.CreateAsync(new CreateObservationCategoryDto("Legal", "  Asuntos legales  "), Guid.NewGuid());

        Assert.True(resultado.Success);
        Assert.Equal("Asuntos legales", resultado.Data!.Description);
    }

    [Fact]
    public async Task CreateAsync_ConDescripcionVacia_GuardaNull()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);

        var resultado = await service.CreateAsync(new CreateObservationCategoryDto("Legal", "   "), Guid.NewGuid());

        Assert.True(resultado.Success);
        Assert.Null(resultado.Data!.Description);
        var guardada = await db.ObservationCategories.SingleAsync(c => c.Name == "Legal");
        Assert.Null(guardada.Description);
    }

    [Fact]
    public async Task CreateAsync_ConNombreExistenteEnOtraCaja_DevuelveDuplicateName()
    {
        using var db = CrearDbContext();
        await CrearCategoriaAsync(db, "Salud");
        var service = new ObservationCategoryService(db);

        var resultado = await service.CreateAsync(new CreateObservationCategoryDto("salud"), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(CategoryOperationError.DuplicateName, resultado.Error);
    }

    [Fact]
    public async Task CreateAsync_RegistraAuditLogConElActor()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);
        var actorId = Guid.NewGuid();

        var resultado = await service.CreateAsync(new CreateObservationCategoryDto("Salud"), actorId);

        Assert.True(resultado.Success);
        var log = await db.AuditLogs.SingleAsync(a => a.AffectedEntity == $"ObservationCategory:{resultado.Data!.Id}");
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Categoría de observación creada", log.Action);
    }

    [Fact]
    public async Task UpdateAsync_ConDatosValidos_ActualizaNombreYDescripcion()
    {
        using var db = CrearDbContext();
        var categoria = await CrearCategoriaAsync(db, "Salud", "Vieja descripción");
        var service = new ObservationCategoryService(db);

        var resultado = await service.UpdateAsync(categoria.Id, new UpdateObservationCategoryDto("Bienestar", "Nueva descripción"), Guid.NewGuid());

        Assert.True(resultado.Success);
        Assert.Equal("Bienestar", resultado.Data!.Name);
        Assert.Equal("Nueva descripción", resultado.Data.Description);
        var actualizada = await db.ObservationCategories.FindAsync(categoria.Id);
        Assert.Equal("Bienestar", actualizada!.Name);
        Assert.Equal("Nueva descripción", actualizada.Description);
    }

    [Fact]
    public async Task UpdateAsync_ConDescripcionVacia_GuardaNull()
    {
        using var db = CrearDbContext();
        var categoria = await CrearCategoriaAsync(db, "Salud", "Vieja descripción");
        var service = new ObservationCategoryService(db);

        var resultado = await service.UpdateAsync(categoria.Id, new UpdateObservationCategoryDto("Salud", "   "), Guid.NewGuid());

        Assert.True(resultado.Success);
        Assert.Null(resultado.Data!.Description);
    }

    [Fact]
    public async Task UpdateAsync_ConIdInexistente_DevuelveNotFound()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);

        var resultado = await service.UpdateAsync(Guid.NewGuid(), new UpdateObservationCategoryDto("Salud"), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(CategoryOperationError.NotFound, resultado.Error);
    }

    [Fact]
    public async Task UpdateAsync_ConNombreDuplicadoDeOtraCategoria_DevuelveDuplicateName()
    {
        using var db = CrearDbContext();
        var salud = await CrearCategoriaAsync(db, "Salud");
        await CrearCategoriaAsync(db, "Legal");
        var service = new ObservationCategoryService(db);

        var resultado = await service.UpdateAsync(salud.Id, new UpdateObservationCategoryDto("legal"), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(CategoryOperationError.DuplicateName, resultado.Error);
    }

    [Fact]
    public async Task UpdateAsync_RegistraAuditLogConElActor()
    {
        using var db = CrearDbContext();
        var categoria = await CrearCategoriaAsync(db, "Salud");
        var service = new ObservationCategoryService(db);
        var actorId = Guid.NewGuid();

        await service.UpdateAsync(categoria.Id, new UpdateObservationCategoryDto("Bienestar"), actorId);

        var log = await db.AuditLogs.SingleAsync(a => a.AffectedEntity == $"ObservationCategory:{categoria.Id}");
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Categoría de observación actualizada", log.Action);
    }

    [Fact]
    public async Task ToggleStatusAsync_DesactivaCategoria_SinBorradoFisico()
    {
        using var db = CrearDbContext();
        var categoria = await CrearCategoriaAsync(db);
        var service = new ObservationCategoryService(db);

        var resultado = await service.ToggleStatusAsync(categoria.Id, false, Guid.NewGuid());

        Assert.True(resultado.Success);
        Assert.True(await db.ObservationCategories.AnyAsync(c => c.Id == categoria.Id));
        var desactivada = await db.ObservationCategories.FindAsync(categoria.Id);
        Assert.False(desactivada!.IsActive);
    }

    [Fact]
    public async Task ToggleStatusAsync_DesactivadaLuegoReactivada_DevuelveActiva()
    {
        using var db = CrearDbContext();
        var categoria = await CrearCategoriaAsync(db);
        var service = new ObservationCategoryService(db);

        await service.ToggleStatusAsync(categoria.Id, false, Guid.NewGuid());
        var reactivacion = await service.ToggleStatusAsync(categoria.Id, true, Guid.NewGuid());

        Assert.True(reactivacion.Success);
        var categoriaFinal = await db.ObservationCategories.FindAsync(categoria.Id);
        Assert.True(categoriaFinal!.IsActive);
    }

    [Fact]
    public async Task ToggleStatusAsync_ConIdInexistente_DevuelveNotFound()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);

        var resultado = await service.ToggleStatusAsync(Guid.NewGuid(), false, Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(CategoryOperationError.NotFound, resultado.Error);
    }

    [Fact]
    public async Task ToggleStatusAsync_RegistraAuditLogConLaAccionDeDesactivacion()
    {
        using var db = CrearDbContext();
        var categoria = await CrearCategoriaAsync(db);
        var service = new ObservationCategoryService(db);
        var actorId = Guid.NewGuid();

        await service.ToggleStatusAsync(categoria.Id, false, actorId);

        var log = await db.AuditLogs.SingleAsync(a => a.AffectedEntity == $"ObservationCategory:{categoria.Id}");
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Categoría de observación desactivada", log.Action);
    }

    [Fact]
    public async Task GetCategoriesAsync_OnlyActive_FiltraSoloActivas()
    {
        using var db = CrearDbContext();
        var activa = await CrearCategoriaAsync(db, "Salud");
        var inactiva = await CrearCategoriaAsync(db, "Legal");
        inactiva.IsActive = false;
        await db.SaveChangesAsync();
        var service = new ObservationCategoryService(db);

        var activas = await service.GetCategoriesAsync(true);

        Assert.Contains(activa.Id, activas.Select(c => c.Id));
        Assert.DoesNotContain(inactiva.Id, activas.Select(c => c.Id));
    }

    [Fact]
    public async Task GetCategoriesAsync_OnlyActiveFalse_IncluyeInactivas()
    {
        using var db = CrearDbContext();
        var activa = await CrearCategoriaAsync(db, "Salud");
        var inactiva = await CrearCategoriaAsync(db, "Legal");
        inactiva.IsActive = false;
        await db.SaveChangesAsync();
        var service = new ObservationCategoryService(db);

        var todas = await service.GetCategoriesAsync(false);

        Assert.Equal(2, todas.Count);
        Assert.Contains(activa.Id, todas.Select(c => c.Id));
        Assert.Contains(inactiva.Id, todas.Select(c => c.Id));
    }
}