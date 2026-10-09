using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Collaborators;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Collaborators;

public class CollaboratorGetByIdTests
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

    private static async Task<User> SeedUserAsync(VicariaDbContext db, string firstName = "Lucía", string lastName = "Gómez")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = $"{Guid.NewGuid()}@mail.com",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetByIdAsync_WhenCollaboratorExists_ReturnsAllDetailsAndRegistrarName()
    {
        using var db = CreateDbContext();
        var user = await SeedUserAsync(db, "Mariana", "Fernández");

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FirstName = "Carlos",
            LastName = "López",
            Dni = "34567890",
            Phone = "3514455667",
            Email = "carlos@test.com",
            Type = CollaboratorType.Volunteer,
            WorkArea = "Cocina",
            IsActive = true,
            RegisteredByUserId = user.Id,
            RegisteredAt = DateTime.UtcNow
        };
        db.Collaborators.Add(collaborator);
        await db.SaveChangesAsync();

        var service = new CollaboratorService(db);

        var result = await service.GetByIdAsync(collaborator.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Collaborator);
        Assert.Equal(collaborator.Id, result.Collaborator.Id);
        Assert.Equal("Carlos", result.Collaborator.FirstName);
        Assert.Equal("López", result.Collaborator.LastName);
        Assert.Equal("34567890", result.Collaborator.Dni);
        Assert.Equal("3514455667", result.Collaborator.Phone);
        Assert.Equal("carlos@test.com", result.Collaborator.Email);
        Assert.Equal(CollaboratorType.Volunteer, result.Collaborator.Type);
        Assert.Equal("Cocina", result.Collaborator.WorkArea);
        Assert.True(result.Collaborator.IsActive);
        Assert.Equal(collaborator.RegisteredAt, result.Collaborator.RegisteredAt);
        Assert.Equal(user.Id, result.Collaborator.RegisteredByUserId);
        Assert.Equal("Mariana Fernández", result.Collaborator.RegisteredByUserName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCollaboratorDoesNotExist_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var service = new CollaboratorService(db);

        var result = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Collaborator);
        Assert.Equal("Colaborador no encontrado.", result.ErrorMessage);
    }
}