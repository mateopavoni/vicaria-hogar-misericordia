using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Vicaria.Api.Controllers;
using Vicaria.Application.Activities;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Activities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Activities;

public class ActivityServiceTests
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
    public async Task CreateAsync_DatosValidos_CreaActividadYRegistraAuditLog()
    {
        using var db = CrearDbContext();
        var service = new ActivityService(db);
        var actorId = Guid.NewGuid();
        var dto = new CreateActivityDto("Taller de Carpintería");

        var result = await service.CreateAsync(dto, actorId);

        Assert.NotNull(result.ActivityId);
        Assert.Null(result.Error);

        var actividad = await db.Activities.SingleAsync(a => a.Id == result.ActivityId);
        Assert.Equal("Taller de Carpintería", actividad.Name);
        Assert.True(actividad.IsActive);
        Assert.False(actividad.IsFromCalendar);

        var auditLog = await db.AuditLogs.FirstOrDefaultAsync(l => l.AffectedEntity == $"Activity:{actividad.Id}");
        Assert.NotNull(auditLog);
        Assert.Equal(actorId, auditLog.UserId);
    }

    [Fact]
    public async Task CreateAsync_NombreDuplicadoActivo_DevuelveDuplicate()
    {
        using var db = CrearDbContext();
        var service = new ActivityService(db);
        var actorId = Guid.NewGuid();
        await service.CreateAsync(new CreateActivityDto("Apoyo Escolar"), actorId);

        var duplicateResult = await service.CreateAsync(new CreateActivityDto("apoyo escolar"), actorId);

        Assert.Equal(CreateActivityError.DuplicateName, duplicateResult.Error);
        Assert.Equal(1, await db.Activities.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_ModificaNombreYEstado_RegistraAuditLog()
    {
        using var db = CrearDbContext();
        var service = new ActivityService(db);
        var actorId = Guid.NewGuid();
        var createResult = await service.CreateAsync(new CreateActivityDto("Panadería"), actorId);

        var updateResult = await service.UpdateAsync(createResult.ActivityId!.Value, new UpdateActivityDto("Taller de Panadería", true), actorId);

        Assert.True(updateResult.IsSuccess);
        var actividad = await db.Activities.SingleAsync(a => a.Id == createResult.ActivityId.Value);
        Assert.Equal("Taller de Panadería", actividad.Name);

        var auditLogs = await db.AuditLogs.Where(l => l.AffectedEntity == $"Activity:{actividad.Id}").ToListAsync();
        Assert.Equal(2, auditLogs.Count);
    }

    [Fact]
    public async Task DeleteAsync_AplicaBajaLogica_NoBorraRegistroYRegistraAuditLog()
    {
        using var db = CrearDbContext();
        var service = new ActivityService(db);
        var actorId = Guid.NewGuid();
        var createResult = await service.CreateAsync(new CreateActivityDto("Costura"), actorId);

        var deleteResult = await service.DeleteAsync(createResult.ActivityId!.Value, actorId);

        Assert.True(deleteResult.IsSuccess);
        var actividad = await db.Activities.SingleAsync(a => a.Id == createResult.ActivityId.Value);
        Assert.False(actividad.IsActive);

        var auditLog = await db.AuditLogs.OrderByDescending(l => l.Date).FirstOrDefaultAsync(l => l.AffectedEntity == $"Activity:{actividad.Id}");
        Assert.NotNull(auditLog);
        Assert.Contains("dada de baja", auditLog.Action);
    }

    [Fact]
    public async Task ListAsync_FiltraInactivasPorDefecto()
    {
        using var db = CrearDbContext();
        var service = new ActivityService(db);
        var actorId = Guid.NewGuid();
        var c1 = await service.CreateAsync(new CreateActivityDto("Activa 1"), actorId);
        var c2 = await service.CreateAsync(new CreateActivityDto("Activa 2"), actorId);
        await service.DeleteAsync(c2.ActivityId!.Value, actorId);

        var listActivas = await service.ListAsync(includeInactive: false);
        var listTodas = await service.ListAsync(includeInactive: true);

        Assert.Single(listActivas);
        Assert.Equal("Activa 1", listActivas[0].Name);
        Assert.Equal(2, listTodas.Count);
    }

    [Fact]
    public void ActivitiesController_RolesCorrectosPorEndpoint()
    {
        var controllerType = typeof(ActivitiesController);

        var createMethod = controllerType.GetMethod(nameof(ActivitiesController.Create));
        var createAuth = createMethod?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(createAuth);
        Assert.Equal(RoleNames.Referent, createAuth.Roles);

        var updateMethod = controllerType.GetMethod(nameof(ActivitiesController.Update));
        var updateAuth = updateMethod?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(updateAuth);
        Assert.Equal(RoleNames.Referent, updateAuth.Roles);

        var deleteMethod = controllerType.GetMethod(nameof(ActivitiesController.Delete));
        var deleteAuth = deleteMethod?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(deleteAuth);
        Assert.Equal(RoleNames.Referent, deleteAuth.Roles);

        var listMethod = controllerType.GetMethod(nameof(ActivitiesController.List));
        var listAuth = listMethod?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(listAuth);
        Assert.Contains(RoleNames.Referent, listAuth.Roles);
        Assert.Contains(RoleNames.CasaConvivenciaDirector, listAuth.Roles);
        Assert.Contains(RoleNames.CasaConvivenciaCoordinator, listAuth.Roles);
    }
}