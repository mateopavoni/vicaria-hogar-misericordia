using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Collaborators;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Collaborators;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Collaborators;

public class CollaboratorUpdateTests
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

    private static async Task<Collaborator> SeedCollaboratorAsync(
        VicariaDbContext db,
        Guid registeredByUserId,
        string firstName = "Juan",
        string? lastName = "Pérez",
        string? dni = "30123456",
        bool isActive = true)
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Dni = dni,
            Type = CollaboratorType.Volunteer,
            WorkArea = "Comedor",
            IsActive = isActive,
            RegisteredByUserId = registeredByUserId,
            RegisteredAt = DateTime.UtcNow
        };
        db.Collaborators.Add(collaborator);
        await db.SaveChangesAsync();
        return collaborator;
    }

    private static UpdateCollaboratorDto NewUpdateDto(
        string firstName = "Juan Carlos",
        string? lastName = "Pérez Gómez",
        string? dni = "30123456",
        string? phone = "1144445555",
        string? email = "juancarlos@mail.com",
        CollaboratorType type = CollaboratorType.Employee,
        string? workArea = "Administración",
        bool isActive = true) =>
        new(firstName, lastName, dni, phone, email, type, workArea, isActive);

    [Fact]
    public async Task UpdateAsync_WithValidDto_UpdatesFieldsAndTrimsValues()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var existing = await SeedCollaboratorAsync(db, actorId);
        var service = new CollaboratorService(db);

        var dto = new UpdateCollaboratorDto(
            FirstName: "  Martín  ",
            LastName: "  González  ",
            Dni: " 35.987.654 ",
            Phone: " 1122334455 ",
            Email: " martin@mail.com ",
            Type: CollaboratorType.Employee,
            WorkArea: " Ropería ",
            IsActive: true);

        var result = await service.UpdateAsync(existing.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.Collaborators.SingleAsync(c => c.Id == existing.Id);
        Assert.Equal("Martín", updated.FirstName);
        Assert.Equal("González", updated.LastName);
        Assert.Equal("35987654", updated.Dni);
        Assert.Equal("1122334455", updated.Phone);
        Assert.Equal("martin@mail.com", updated.Email);
        Assert.Equal(CollaboratorType.Employee, updated.Type);
        Assert.Equal("Ropería", updated.WorkArea);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WhenKeepingSameDni_Succeeds()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var existing = await SeedCollaboratorAsync(db, actorId, dni: "30123456");
        var service = new CollaboratorService(db);

        var dto = NewUpdateDto(dni: "30123456");

        var result = await service.UpdateAsync(existing.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new CollaboratorService(db);

        var result = await service.UpdateAsync(Guid.NewGuid(), NewUpdateDto(), actorId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateCollaboratorError.NotFound, result.Error);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateDniBelongingToAnotherCollaborator_ReturnsDuplicateDni()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var collaborator1 = await SeedCollaboratorAsync(db, actorId, firstName: "Uno", dni: "30123456");
        var collaborator2 = await SeedCollaboratorAsync(db, actorId, firstName: "Dos", dni: "40999888");
        var service = new CollaboratorService(db);
        var dto = NewUpdateDto(dni: "30.123.456");

        var result = await service.UpdateAsync(collaborator2.Id, dto, actorId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateCollaboratorError.DuplicateDni, result.Error);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task UpdateAsync_WhenDeactivating_WritesDeactivatedAuditLog()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var existing = await SeedCollaboratorAsync(db, actorId, isActive: true);
        var service = new CollaboratorService(db);

        var dto = NewUpdateDto(isActive: false);

        var result = await service.UpdateAsync(existing.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Colaborador dado de baja", log.Action);
        Assert.Equal($"Collaborator:{existing.Id}", log.AffectedEntity);
    }

    [Fact]
    public async Task UpdateAsync_WhenReactivating_WritesReactivatedAuditLog()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var existing = await SeedCollaboratorAsync(db, actorId, isActive: false);
        var service = new CollaboratorService(db);

        var dto = NewUpdateDto(isActive: true);

        var result = await service.UpdateAsync(existing.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Colaborador reactivado", log.Action);
        Assert.Equal($"Collaborator:{existing.Id}", log.AffectedEntity);
    }

    [Fact]
    public async Task UpdateAsync_WhenOnlyModifyingData_WritesModifiedAuditLog()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var existing = await SeedCollaboratorAsync(db, actorId, isActive: true);
        var service = new CollaboratorService(db);

        var dto = NewUpdateDto(isActive: true);

        var result = await service.UpdateAsync(existing.Id, dto, actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Colaborador modificado", log.Action);
        Assert.Equal($"Collaborator:{existing.Id}", log.AffectedEntity);
    }
}