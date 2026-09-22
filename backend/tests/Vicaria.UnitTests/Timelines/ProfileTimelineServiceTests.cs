using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Timelines;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.Timelines;

namespace Vicaria.UnitTests.Timelines;

public class ProfileTimelineServiceTests
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
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Ana", CreatedAt = DateTime.UtcNow };
        db.People.Add(person);
        await db.SaveChangesAsync();
        return person.Id;
    }

    private static async Task<Guid> CrearAutorAsync(VicariaDbContext db)
    {
        var author = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "María",
            LastName = "López",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "x",
            Status = UserStatus.Active,
            RoleId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(author);
        await db.SaveChangesAsync();
        return author.Id;
    }

    [Fact]
    public async Task GetTimelineAsync_PersonaInexistente_DevuelveError()
    {
        using var db = CrearDbContext();
        var service = new ProfileTimelineService(db);

        var result = await service.GetTimelineAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(ProfileTimelineError.PersonNotFound, result.Error);
    }

    [Fact]
    public async Task GetTimelineAsync_SinDatos_DevuelveListaVacia()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaAsync(db);
        var service = new ProfileTimelineService(db);

        var result = await service.GetTimelineAsync(personId);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!.Items);
        Assert.Equal(0, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetTimelineAsync_CombinaObservacionesYEstadias_OrdenadasPorFecha()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaAsync(db);
        var authorId = await CrearAutorAsync(db);

        var category = new ObservationCategory { Id = Guid.NewGuid(), Name = "Salud", IsActive = true };
        db.ObservationCategories.Add(category);

        db.Observations.Add(new Observation
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Content = "Evoluciona bien",
            CategoryId = category.Id,
            AuthorUserId = authorId,
            CreatedAt = DateTime.UtcNow
        });

        var stay = new CasonaStay
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            EntryDate = DateTime.UtcNow.AddDays(-10),
            ExitDate = DateTime.UtcNow.AddDays(-3),
            ExitReason = StayExitReason.VoluntaryDischarge
        };
        db.CasonaStays.Add(stay);

        await db.SaveChangesAsync();

        var service = new ProfileTimelineService(db);
        var result = await service.GetTimelineAsync(personId);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data!.TotalCount);
        Assert.Equal(3, result.Data.Items.Count);

        // orden descendente por fecha
        for (var i = 1; i < result.Data.Items.Count; i++)
        {
            Assert.True(result.Data.Items[i - 1].Date >= result.Data.Items[i].Date);
        }

        Assert.Contains(result.Data.Items, e => e.Type == ProfileTimelineEntryType.Observation && e.Content == "Evoluciona bien" && e.CategoryName == "Salud");
        Assert.Contains(result.Data.Items, e => e.Type == ProfileTimelineEntryType.CasonaStayEntry && e.Title == "Ingreso a la Casona");
        Assert.Contains(result.Data.Items, e => e.Type == ProfileTimelineEntryType.CasonaStayExit && e.Title == "Egreso de la Casona");
    }

    [Fact]
    public async Task GetTimelineAsync_EstadiaSinEgreso_SoloGeneraIngreso()
    {
        using var db = CrearDbContext();
        var personId = await CrearPersonaAsync(db);

        db.CasonaStays.Add(new CasonaStay
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            EntryDate = DateTime.UtcNow.AddDays(-5)
        });

        await db.SaveChangesAsync();

        var service = new ProfileTimelineService(db);
        var result = await service.GetTimelineAsync(personId);

        Assert.True(result.Success);
        Assert.Single(result.Data!.Items);
        Assert.Equal(ProfileTimelineEntryType.CasonaStayEntry, result.Data.Items[0].Type);
    }
}