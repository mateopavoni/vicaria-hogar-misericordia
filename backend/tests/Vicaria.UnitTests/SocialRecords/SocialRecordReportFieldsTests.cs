using Microsoft.EntityFrameworkCore;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.SocialRecords;

namespace Vicaria.UnitTests.SocialRecords;

public class SocialRecordReportFieldsTests
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

    private static async Task<SocialRecord> SeedRecordAsync(VicariaDbContext context)
    {
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Mario", LastName = "Bros" };
        var record = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.People.Add(person);
        context.SocialRecords.Add(record);
        await context.SaveChangesAsync();
        return record;
    }

    [Fact]
    public async Task UpdateReportFieldsAsync_ConValoresValidos_LosGuardaYRegistraAuditLog()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);
        var record = await SeedRecordAsync(context);
        var gender = Value(CaritasCatalogCategory.Gender, "Femenino");
        var reason = Value(CaritasCatalogCategory.AttendanceReason, "Consumo de alcohol");
        context.CaritasCatalogValues.AddRange(gender, reason);
        await context.SaveChangesAsync();
        var actorId = Guid.NewGuid();

        var result = await service.UpdateReportFieldsAsync(record.Id, new SocialRecordReportFieldsDto(gender.Id, null, null, null, reason.Id), actorId);

        Assert.True(result.Success);
        var detail = await service.GetByIdAsync(record.Id);
        Assert.NotNull(detail);
        Assert.Equal(gender.Id, detail.ReportFields.GenderId);
        Assert.Equal(reason.Id, detail.ReportFields.AttendanceReasonId);
        Assert.Null(detail.ReportFields.HousingSituationId);
        var audit = await context.AuditLogs.SingleAsync();
        Assert.Equal(actorId, audit.UserId);
        Assert.Equal($"SocialRecord:{record.Id}", audit.AffectedEntity);
    }

    [Fact]
    public async Task UpdateReportFieldsAsync_ConValorDeOtraCategoria_DevuelveInvalidCatalogValue()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);
        var record = await SeedRecordAsync(context);
        var housing = Value(CaritasCatalogCategory.HousingSituation, "En situación de calle");
        context.CaritasCatalogValues.Add(housing);
        await context.SaveChangesAsync();

        var result = await service.UpdateReportFieldsAsync(record.Id, new SocialRecordReportFieldsDto(housing.Id, null, null, null, null), Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(UpdateReportFieldsError.InvalidCatalogValue, result.Error);
    }

    [Fact]
    public async Task UpdateReportFieldsAsync_ConValorDesactivadoNuevo_LoRechazaPeroConservaElQueYaTenia()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);
        var record = await SeedRecordAsync(context);
        var inactive = Value(CaritasCatalogCategory.Gender, "Otro", isActive: false);
        context.CaritasCatalogValues.Add(inactive);
        await context.SaveChangesAsync();

        var rejected = await service.UpdateReportFieldsAsync(record.Id, new SocialRecordReportFieldsDto(inactive.Id, null, null, null, null), Guid.NewGuid());
        Assert.Equal(UpdateReportFieldsError.InvalidCatalogValue, rejected.Error);

        record.GenderId = inactive.Id;
        await context.SaveChangesAsync();
        var kept = await service.UpdateReportFieldsAsync(record.Id, new SocialRecordReportFieldsDto(inactive.Id, null, null, null, null), Guid.NewGuid());
        Assert.True(kept.Success);
    }

    [Fact]
    public async Task UpdateReportFieldsAsync_ConFichaInexistente_DevuelveNotFound()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);

        var result = await service.UpdateReportFieldsAsync(Guid.NewGuid(), new SocialRecordReportFieldsDto(null, null, null, null, null), Guid.NewGuid());

        Assert.Equal(UpdateReportFieldsError.NotFound, result.Error);
    }
}
