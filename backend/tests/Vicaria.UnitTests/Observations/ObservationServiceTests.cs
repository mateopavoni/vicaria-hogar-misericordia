using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Observations;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Observations;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Observations;

public class ObservationServiceTests
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
    public async Task ExportToCsvAsync_DevuelveSoloLasObservacionesQueCoincidenConElFiltro()
    {
        using var db = CrearDbContext();
        var personId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();

        db.ObservationCategories.Add(new ObservationCategory { Id = categoriaId, Name = "Salud", IsActive = true });
        db.Observations.AddRange(
            new Observation { Id = Guid.NewGuid(), PersonId = personId, CategoryId = categoriaId, AuthorUserId = authorId, Content = "Con categoría", CreatedAt = DateTime.UtcNow },
            new Observation { Id = Guid.NewGuid(), PersonId = personId, AuthorUserId = authorId, Content = "Sin categoría", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Observation { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), AuthorUserId = authorId, Content = "De otra persona", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = new ObservationService(db);

        var csv = await service.ExportToCsvAsync(personId, new GetObservationsFilterDto(CategoryId: categoriaId));

        Assert.Contains("Con categoría", csv);
        Assert.DoesNotContain("Sin categoría", csv);
        Assert.DoesNotContain("De otra persona", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_EscapaComasYComillasEnElContenido()
    {
        using var db = CrearDbContext();
        var personId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        db.Observations.Add(new Observation
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            AuthorUserId = authorId,
            Content = "Contenido con \"comillas\", y una coma",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ObservationService(db);

        var csv = await service.ExportToCsvAsync(personId, new GetObservationsFilterDto());

        Assert.Contains("\"Contenido con \"\"comillas\"\", y una coma\"", csv);
    }

    [Fact]
    public async Task ExportToCsvAsync_SinObservaciones_DevuelveSoloElEncabezado()
    {
        using var db = CrearDbContext();
        var service = new ObservationService(db);

        var csv = await service.ExportToCsvAsync(Guid.NewGuid(), new GetObservationsFilterDto());

        Assert.Equal("Fecha,Categoría,Autor,Contenido" + Environment.NewLine, csv);
    }
}
