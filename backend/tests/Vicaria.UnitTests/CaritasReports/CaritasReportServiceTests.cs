using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CaritasReports;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CaritasReports;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CaritasReports;

public class CaritasReportServiceTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    private static CaritasCatalogValue Value(CaritasCatalogCategory category, string name, bool isActive = true) =>
        new() { Id = Guid.NewGuid(), Category = category, Name = name, IsActive = isActive, CreatedAt = DateTime.UtcNow };

    // persona con asistencia ayer y los campos del informe indicados
    private static async Task SeedAttendedRecordAsync(VicariaDbContext context, Guid? genderId = null, Guid? housingId = null, Guid? originId = null)
    {
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Ana" };
        context.People.Add(person);
        context.SocialRecords.Add(new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            GenderId = genderId,
            HousingSituationId = housingId,
            OriginLocalityId = originId,
            CreatedAt = DateTime.UtcNow.AddDays(-200),
            UpdatedAt = DateTime.UtcNow
        });
        context.Attendances.Add(new Attendance
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Date = DateTime.UtcNow.Date.AddDays(-1),
            CreatedByUserId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetReportAsync_CadaCategoriaSumaElTotal_ConNoInformado()
    {
        using var context = CreateInMemoryDbContext();
        var street = Value(CaritasCatalogCategory.HousingSituation, "En situación de calle");
        var housed = Value(CaritasCatalogCategory.HousingSituation, "Con domicilio");
        context.CaritasCatalogValues.AddRange(street, housed);
        await SeedAttendedRecordAsync(context, housingId: street.Id);
        await SeedAttendedRecordAsync(context, housingId: housed.Id);
        await SeedAttendedRecordAsync(context);
        var service = new CaritasReportService(context);

        var result = await service.GetReportAsync("trimestral", DateTime.UtcNow.Date);

        Assert.True(result.Success);
        var report = result.Data!;
        Assert.Equal(3, report.Totals.AttendedPersons);
        foreach (var category in new[] { report.Gender, report.Housing, report.Dni, report.Geography, report.Reasons })
        {
            Assert.Equal(3, category.Sum(i => i.Count));
        }
        Assert.Contains(report.Housing, i => i.Label == "No informado" && i.Count == 1);
    }

    [Fact]
    public async Task GetReportAsync_CuentaValoresNuevosYRenombradosPorId()
    {
        using var context = CreateInMemoryDbContext();
        var renamed = Value(CaritasCatalogCategory.Gender, "Mujer");
        var added = Value(CaritasCatalogCategory.Gender, "No binario");
        context.CaritasCatalogValues.AddRange(renamed, added);
        await SeedAttendedRecordAsync(context, genderId: renamed.Id);
        await SeedAttendedRecordAsync(context, genderId: added.Id);
        var service = new CaritasReportService(context);

        var report = (await service.GetReportAsync("trimestral", DateTime.UtcNow.Date)).Data!;

        Assert.Contains(report.Gender, i => i.Label == "Mujer" && i.Count == 1);
        Assert.Contains(report.Gender, i => i.Label == "No binario" && i.Count == 1);
        Assert.DoesNotContain(report.Gender, i => i.Label == "No informado");
    }

    [Fact]
    public async Task GetReportAsync_ValorDadoDeBajaSoloApareceSiSeUsa()
    {
        using var context = CreateInMemoryDbContext();
        var activeUnused = Value(CaritasCatalogCategory.AttendanceReason, "Violencia");
        var inactiveUnused = Value(CaritasCatalogCategory.AttendanceReason, "Viejo motivo", isActive: false);
        var inactiveUsed = Value(CaritasCatalogCategory.Gender, "Otro", isActive: false);
        context.CaritasCatalogValues.AddRange(activeUnused, inactiveUnused, inactiveUsed);
        await SeedAttendedRecordAsync(context, genderId: inactiveUsed.Id);
        var service = new CaritasReportService(context);

        var report = (await service.GetReportAsync("trimestral", DateTime.UtcNow.Date)).Data!;

        Assert.Contains(report.Reasons, i => i.Label == "Violencia" && i.Count == 0);
        Assert.DoesNotContain(report.Reasons, i => i.Label == "Viejo motivo");
        Assert.Contains(report.Gender, i => i.Label == "Otro" && i.Count == 1);
    }

    [Fact]
    public async Task GetReportAsync_GeografiaSoloLocalidadesConPersonas()
    {
        using var context = CreateInMemoryDbContext();
        var cordoba = Value(CaritasCatalogCategory.OriginLocality, "Córdoba");
        var empty = Value(CaritasCatalogCategory.OriginLocality, "Rosario");
        context.CaritasCatalogValues.AddRange(cordoba, empty);
        await SeedAttendedRecordAsync(context, originId: cordoba.Id);
        await SeedAttendedRecordAsync(context, originId: cordoba.Id);
        var service = new CaritasReportService(context);

        var report = (await service.GetReportAsync("trimestral", DateTime.UtcNow.Date)).Data!;

        var item = Assert.Single(report.Geography);
        Assert.Equal("Córdoba", item.Label);
        Assert.Equal(2, item.Count);
    }

    [Fact]
    public void GenerateReportPdf_ConMuchasLocalidades_GeneraVariasPaginas()
    {
        var geography = Enumerable.Range(1, 60)
            .Select(i => new CaritasReportCountItemDto($"Localidad {i}", 1))
            .ToList();
        var empty = new List<CaritasReportCountItemDto>();
        var report = new CaritasReportDto(
            "trimestral", "2026-07-01", "2026-09-30", false,
            new CaritasReportTotalsDto(60, 1, 0),
            empty, empty, empty, geography, empty);

        var bytes = new CaritasReportPdfGenerator().GenerateReportPdf(report);

        using var stream = new MemoryStream(bytes);
        using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount >= 2);
    }
}
