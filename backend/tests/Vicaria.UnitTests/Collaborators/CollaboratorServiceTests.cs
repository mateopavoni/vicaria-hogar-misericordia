using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Collaborators;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Collaborators;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Collaborators;

public class CollaboratorServiceTests
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

    private static async Task<Collaborator> SeedCollaboratorAsync(VicariaDbContext db, Guid registeredByUserId, string? dni)
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FirstName = "Colaborador",
            Dni = dni,
            Type = CollaboratorType.Volunteer,
            RegisteredByUserId = registeredByUserId,
            RegisteredAt = DateTime.UtcNow
        };
        db.Collaborators.Add(collaborator);
        await db.SaveChangesAsync();
        return collaborator;
    }

    private static CreateCollaboratorDto NewDto(
        string firstName = "María",
        string? lastName = null,
        string? dni = null,
        string? phone = null,
        string? email = null,
        CollaboratorType type = CollaboratorType.Volunteer,
        string? workArea = null) =>
        new(firstName, lastName, dni, phone, email, type, workArea);

    [Fact]
    public async Task CreateAsync_WithValidDto_PersistsCollaboratorWithActorAndTrimmedValues()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new CollaboratorService(db);

        var result = await service.CreateAsync(
            NewDto(
                firstName: "  María  ",
                lastName: "  Pérez  ",
                dni: " 30.123.456 ",
                phone: " 1145678901 ",
                email: " maria@mail.com ",
                type: CollaboratorType.Employee,
                workArea: " Comedor "),
            actorId,
            CancellationToken.None);

        Assert.True(result.Success);
        var stored = db.Collaborators.Single(c => c.Id == result.CollaboratorId);
        Assert.Equal("María", stored.FirstName);
        Assert.Equal("Pérez", stored.LastName);
        Assert.Equal("30123456", stored.Dni);
        Assert.Equal("1145678901", stored.Phone);
        Assert.Equal("maria@mail.com", stored.Email);
        Assert.Equal(CollaboratorType.Employee, stored.Type);
        Assert.Equal("Comedor", stored.WorkArea);
        Assert.Equal(actorId, stored.RegisteredByUserId);
        Assert.Equal(DateTimeKind.Utc, stored.RegisteredAt.Kind);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_WritesAuditLogEntry()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new CollaboratorService(db);

        var result = await service.CreateAsync(NewDto(), actorId, CancellationToken.None);

        Assert.True(result.Success);
        var log = db.AuditLogs.Single();
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Colaborador creado", log.Action);
        Assert.Equal($"Collaborator:{result.CollaboratorId}", log.AffectedEntity);
        Assert.Equal(DateTimeKind.Utc, log.Date.Kind);
    }

    [Fact]
    public async Task CreateAsync_WithNullOptionalFields_PersistsNulls()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new CollaboratorService(db);

        var result = await service.CreateAsync(NewDto(), actorId, CancellationToken.None);

        Assert.True(result.Success);
        var stored = db.Collaborators.Single(c => c.Id == result.CollaboratorId);
        Assert.Null(stored.LastName);
        Assert.Null(stored.Dni);
        Assert.Null(stored.Phone);
        Assert.Null(stored.Email);
        Assert.Null(stored.WorkArea);
    }

    [Fact]
    public async Task CreateAsync_WithNullDni_Twice_BothPersist()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new CollaboratorService(db);

        var first = await service.CreateAsync(NewDto(firstName: "Ana"), actorId, CancellationToken.None);
        var second = await service.CreateAsync(NewDto(firstName: "Luis"), actorId, CancellationToken.None);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, db.Collaborators.Count());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateDni_ReturnsDuplicateDniAndPersistsNothing()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "30123456");
        var service = new CollaboratorService(db);

        var result = await service.CreateAsync(NewDto(dni: "30123456"), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateCollaboratorError.DuplicateDni, result.Error);
        Assert.Equal("Ya existe un colaborador con ese DNI.", result.ErrorMessage);
        Assert.Equal(1, db.Collaborators.Count());
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task CreateAsync_WithSameDniInDifferentFormat_ReturnsDuplicateDni()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "30123456");
        var service = new CollaboratorService(db);

        var result = await service.CreateAsync(NewDto(dni: "30.123.456"), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateCollaboratorError.DuplicateDni, result.Error);
        Assert.Equal(1, db.Collaborators.Count());
    }

    [Fact]
    public async Task CreateAsync_WithDifferentDni_Succeeds()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "30123456");
        var service = new CollaboratorService(db);

        var result = await service.CreateAsync(NewDto(dni: "28987654"), actorId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, db.Collaborators.Count());
    }
}
