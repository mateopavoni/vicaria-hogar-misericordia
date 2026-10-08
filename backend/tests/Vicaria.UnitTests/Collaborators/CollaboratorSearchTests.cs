using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Collaborators;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Collaborators;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Collaborators;

public class CollaboratorSearchTests
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
        string firstName,
        string? lastName = null,
        CollaboratorType type = CollaboratorType.Volunteer,
        string? workArea = null)
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Type = type,
            WorkArea = workArea,
            RegisteredByUserId = registeredByUserId,
            RegisteredAt = DateTime.UtcNow
        };
        db.Collaborators.Add(collaborator);
        await db.SaveChangesAsync();
        return collaborator;
    }

    [Fact]
    public async Task SearchAsync_WithPartialFirstName_FindsCollaborator()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "Ramón", "Gómez");
        var service = new CollaboratorService(db);

        var results = await service.SearchAsync("ram");

        var result = Assert.Single(results);
        Assert.Equal("Ramón Gómez", result.FullName);
    }

    [Fact]
    public async Task SearchAsync_IgnoresAccentsAndCase()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "Joaquín", "Álvarez");
        var service = new CollaboratorService(db);

        var withoutAccents = await service.SearchAsync("JOAQUIN");
        var upperWithAccents = await service.SearchAsync("JOAQUÍN");

        Assert.Single(withoutAccents);
        Assert.Single(upperWithAccents);
    }

    [Fact]
    public async Task SearchAsync_MatchesLastNameAndWorkArea()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "Ana", "Pérez", workArea: "Comedor");
        await SeedCollaboratorAsync(db, actorId, "Luis", "Gómez", workArea: "Ropa");
        var service = new CollaboratorService(db);

        var byLastName = await service.SearchAsync("perez");
        var byWorkArea = await service.SearchAsync("comedor");

        Assert.Single(byLastName);
        Assert.Equal("Ana Pérez", byLastName[0].FullName);
        Assert.Single(byWorkArea);
        Assert.Equal("Comedor", byWorkArea[0].WorkArea);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyOrNullQuery_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "Ana");
        var service = new CollaboratorService(db);

        var empty = await service.SearchAsync("");
        var nullQuery = await service.SearchAsync(null);
        var whitespace = await service.SearchAsync("   ");

        Assert.Empty(empty);
        Assert.Empty(nullQuery);
        Assert.Empty(whitespace);
    }

    [Fact]
    public async Task SearchAsync_WithTypeFilter_ReturnsOnlyThatType()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "Voluntaria", type: CollaboratorType.Volunteer);
        await SeedCollaboratorAsync(db, actorId, "Empleada", type: CollaboratorType.Employee);
        var service = new CollaboratorService(db);

        var volunteers = await service.SearchAsync("a", CollaboratorType.Volunteer);
        var employees = await service.SearchAsync("a", CollaboratorType.Employee);
        var all = await service.SearchAsync("a");

        Assert.Single(volunteers);
        Assert.Equal(CollaboratorType.Volunteer, volunteers[0].Type);
        Assert.Single(employees);
        Assert.Equal(CollaboratorType.Employee, employees[0].Type);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task SearchAsync_WithNullLastName_ComposesFullNameWithFirstNameOnly()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var seeded = await SeedCollaboratorAsync(db, actorId, "SinApellido");
        var service = new CollaboratorService(db);

        var results = await service.SearchAsync("sinapellido");

        var result = Assert.Single(results);
        Assert.Equal(seeded.Id, result.Id);
        Assert.Equal("SinApellido", result.FullName);
    }

    [Fact]
    public async Task SearchAsync_WithLikeWildcards_TreatsThemLiterally()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var literal = await SeedCollaboratorAsync(db, actorId, "Zoe%Alpha");
        await SeedCollaboratorAsync(db, actorId, "Beta");
        var service = new CollaboratorService(db);

        var results = await service.SearchAsync("%");

        var result = Assert.Single(results);
        Assert.Equal(literal.Id, result.Id);
    }

    [Fact]
    public async Task SearchAsync_WithoutMatches_ReturnsEmptyList()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        await SeedCollaboratorAsync(db, actorId, "Ana");
        var service = new CollaboratorService(db);

        var results = await service.SearchAsync("noexiste");

        Assert.Empty(results);
    }
}
