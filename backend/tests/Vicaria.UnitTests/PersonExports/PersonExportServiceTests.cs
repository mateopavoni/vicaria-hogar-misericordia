using System.Text;
using Microsoft.EntityFrameworkCore;
using Vicaria.Application.PersonExports;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Pdf;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.PersonExports;

namespace Vicaria.UnitTests.PersonExports;

// SCRUM-252: documento de exportación de la ficha, auditoría y PDF
public class PersonExportServiceTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    private static async Task<(Guid PersonId, Guid ActorId)> SeedAsync(VicariaDbContext context)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = RoleNames.Referent };
        var actor = new User
        {
            Id = Guid.NewGuid(), FirstName = "Laura", LastName = "Referente", Email = "laura@mail.com",
            PasswordHash = "x", Status = UserStatus.Active, RoleId = role.Id, Role = role, CreatedAt = DateTime.UtcNow
        };
        var otherAuthor = new User
        {
            Id = Guid.NewGuid(), FirstName = "Otro", LastName = "Escucha", Email = "otro@mail.com",
            PasswordHash = "x", Status = UserStatus.Active, RoleId = role.Id, CreatedAt = DateTime.UtcNow
        };
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Juan", LastName = "Pérez", Dni = "30111222" };
        var record = new SocialRecord
        {
            Id = Guid.NewGuid(), PersonId = person.Id, PersonType = PersonType.Resident,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var category = new ObservationCategory { Id = Guid.NewGuid(), Name = "Salud", CreatedAt = DateTime.UtcNow };
        context.AddRange(role, actor, otherAuthor, person, record, category);
        context.Observations.Add(new Observation
        {
            Id = Guid.NewGuid(), PersonId = person.Id, Content = "Control médico al día", CategoryId = category.Id,
            AuthorUserId = otherAuthor.Id, CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        context.LifeStories.Add(new LifeStory { PersonId = person.Id, BeforeCentroBarrial = "Vivía en la calle" });
        await context.SaveChangesAsync();
        return (person.Id, actor.Id);
    }

    [Fact]
    public async Task GetExportAsync_SoloDatos_NoIncluyeObservacionesNiHistoriaYRegistraAuditoria()
    {
        using var context = CreateInMemoryDbContext();
        var (personId, actorId) = await SeedAsync(context);
        var service = new PersonExportService(context);

        var result = await service.GetExportAsync(personId, "datos", actorId);

        Assert.True(result.Success);
        var document = result.Document!;
        Assert.Empty(document.Observations);
        Assert.Null(document.BeforeCentroBarrial);
        Assert.Equal("Residente", document.PersonType);
        Assert.Equal("Laura Referente", document.GeneratedByName);
        Assert.Equal(RoleNames.Referent, document.GeneratedByRole);
        var audit = await context.AuditLogs.SingleAsync();
        Assert.Equal(actorId, audit.UserId);
        Assert.Equal($"Person:{personId}", audit.AffectedEntity);
        Assert.Contains("solo datos personales", audit.Action);
    }

    [Fact]
    public async Task GetExportAsync_Todo_IncluyeObservacionesSinAutorEHistoriaDeVida()
    {
        using var context = CreateInMemoryDbContext();
        var (personId, actorId) = await SeedAsync(context);
        var service = new PersonExportService(context);

        var result = await service.GetExportAsync(personId, "TODO", actorId);

        var document = result.Document!;
        var observation = Assert.Single(document.Observations);
        Assert.Equal("Salud", observation.CategoryName);
        Assert.Equal("Control médico al día", observation.Content);
        Assert.Equal("Vivía en la calle", document.BeforeCentroBarrial);
        Assert.Equal(PersonExportSections.All, document.Sections);
    }

    [Fact]
    public async Task GetExportAsync_SeccionesInvalidas_DevuelveErrorSinAuditoria()
    {
        using var context = CreateInMemoryDbContext();
        var (personId, actorId) = await SeedAsync(context);
        var service = new PersonExportService(context);

        var result = await service.GetExportAsync(personId, "observaciones", actorId);

        Assert.Equal(PersonExportError.InvalidSections, result.Error);
        Assert.Empty(await context.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task GetExportAsync_PersonaInexistente_DevuelvePersonNotFound()
    {
        using var context = CreateInMemoryDbContext();
        var service = new PersonExportService(context);

        var result = await service.GetExportAsync(Guid.NewGuid(), "datos", Guid.NewGuid());

        Assert.Equal(PersonExportError.PersonNotFound, result.Error);
    }

    [Fact]
    public void GenerateExportPdf_TextoLargo_ParteLasLineasYAgregaPaginas()
    {
        PdfFontResolver.Register();
        var longText = string.Join(' ', Enumerable.Repeat("palabra", 2500));
        var document = new PersonExportDocument(
            Guid.NewGuid(), "Juan", "Pérez", "30111222", null, null, "Residente", null, null, null, null, null,
            true, null, PersonExportSections.All,
            [new PersonExportObservationItem(DateTime.UtcNow, "Salud", longText)],
            longText, null, null, "Laura Referente", RoleNames.Referent, DateTime.UtcNow);

        var bytes = new PersonExportPdfGenerator().GenerateExportPdf(document);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 1);
    }
}
