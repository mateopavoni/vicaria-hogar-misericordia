using Microsoft.EntityFrameworkCore;
using Vicaria.Application.ObservationCategories;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.ObservationCategories;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.ObservationCategories;

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

    [Fact]
    public async Task CreateAsync_CreaCategoriaActivaYRegistraAuditLog()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);
        var actorId = Guid.NewGuid();

        var resultado = await service.CreateAsync(new CreateObservationCategoryDto("Convivencia", "Observaciones de convivencia"), actorId);

        Assert.True(resultado.Success);
        var category = await db.ObservationCategories.SingleAsync(c => c.Id == resultado.ObservationCategoryId);
        Assert.Equal("Convivencia", category.Name);
        Assert.Equal("Observaciones de convivencia", category.Description);
        Assert.True(category.IsActive);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a =>
            a.AffectedEntity == $"ObservationCategory:{category.Id}" && a.UserId == actorId);
        Assert.NotNull(log);
    }

    [Fact]
    public async Task CreateAsync_TrimeaNombreYDescripcion()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);

        var resultado = await service.CreateAsync(new CreateObservationCategoryDto("  Convivencia  ", "  notas  "), Guid.NewGuid());

        var category = await db.ObservationCategories.SingleAsync(c => c.Id == resultado.ObservationCategoryId);
        Assert.Equal("Convivencia", category.Name);
        Assert.Equal("notas", category.Description);
    }

    [Fact]
    public async Task UpdateAsync_ActualizaYRegistraAuditLog()
    {
        using var db = CrearDbContext();
        var actorId = Guid.NewGuid();
        var category = new ObservationCategory
        {
            Id = Guid.NewGuid(),
            Name = "Vieja",
            Description = "Vieja desc",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.ObservationCategories.Add(category);
        await db.SaveChangesAsync();
        var service = new ObservationCategoryService(db);

        var resultado = await service.UpdateAsync(category.Id, new UpdateObservationCategoryDto("Nueva", "Nueva desc"), actorId);

        Assert.True(resultado.Success);
        var updated = await db.ObservationCategories.SingleAsync(c => c.Id == category.Id);
        Assert.Equal("Nueva", updated.Name);
        Assert.Equal("Nueva desc", updated.Description);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a =>
            a.AffectedEntity == $"ObservationCategory:{category.Id}" && a.UserId == actorId);
        Assert.NotNull(log);
    }

    [Fact]
    public async Task UpdateAsync_CategoriaInexistente_DevuelveCategoryNotFound()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);

        var resultado = await service.UpdateAsync(Guid.NewGuid(), new UpdateObservationCategoryDto("X", null), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal("La categoría de observación no existe.", resultado.ErrorMessage);
    }

    [Fact]
    public async Task DeactivateAsync_DesactivaSinBorrarFisicamenteYRegistraAuditLog()
    {
        using var db = CrearDbContext();
        var actorId = Guid.NewGuid();
        var category = new ObservationCategory
        {
            Id = Guid.NewGuid(),
            Name = "Convivencia",
            Description = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.ObservationCategories.Add(category);
        await db.SaveChangesAsync();
        var service = new ObservationCategoryService(db);

        var resultado = await service.DeactivateAsync(category.Id, actorId);

        Assert.True(resultado.Success);

        // soft delete: la fila sigue existiendo, solo cambia IsActive
        var deactivated = await db.ObservationCategories.SingleAsync(c => c.Id == category.Id);
        Assert.False(deactivated.IsActive);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a =>
            a.AffectedEntity == $"ObservationCategory:{category.Id}" && a.UserId == actorId);
        Assert.NotNull(log);
    }

    [Fact]
    public async Task DeactivateAsync_CategoriaInexistente_DevuelveCategoryNotFound()
    {
        using var db = CrearDbContext();
        var service = new ObservationCategoryService(db);

        var resultado = await service.DeactivateAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(DeactivateObservationCategoryError.CategoryNotFound, resultado.Error);
    }

    [Fact]
    public async Task DeactivateAsync_YaDesactivada_DevuelveAlreadyDeactivated()
    {
        using var db = CrearDbContext();
        var category = new ObservationCategory
        {
            Id = Guid.NewGuid(),
            Name = "Convivencia",
            Description = null,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        db.ObservationCategories.Add(category);
        await db.SaveChangesAsync();
        var service = new ObservationCategoryService(db);

        var resultado = await service.DeactivateAsync(category.Id, Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(DeactivateObservationCategoryError.AlreadyDeactivated, resultado.Error);
    }
}