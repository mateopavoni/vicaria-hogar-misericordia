using Microsoft.EntityFrameworkCore;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.SocialRecords;

namespace Vicaria.UnitTests.SocialRecords;

public class SocialRecordExportLogTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task LogExportAsync_RegistraAuditLogConActorYSecciones()
    {
        using var context = CreateInMemoryDbContext();
        var record = new SocialRecord { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.SocialRecords.Add(record);
        await context.SaveChangesAsync();
        var service = new SocialRecordService(context);
        var actorId = Guid.NewGuid();

        var result = await service.LogExportAsync(record.Id, new LogRecordExportDto("datos-observaciones", "pdf"), actorId);

        Assert.True(result.Success);
        var audit = await context.AuditLogs.SingleAsync();
        Assert.Equal(actorId, audit.UserId);
        Assert.Equal($"SocialRecord:{record.Id}", audit.AffectedEntity);
        Assert.Contains("PDF", audit.Action);
        Assert.Contains("datos y observaciones", audit.Action);
    }

    [Fact]
    public async Task LogExportAsync_FichaInexistente_DevuelveNotFound()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);

        var result = await service.LogExportAsync(Guid.NewGuid(), new LogRecordExportDto("todo", "print"), Guid.NewGuid());

        Assert.Equal(LogRecordExportError.NotFound, result.Error);
        Assert.Empty(await context.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("todo", "pdf", true)]
    [InlineData("datos", "print", true)]
    [InlineData("todo-todo", "pdf", false)]
    [InlineData("datos", "docx", false)]
    public void Validator_AceptaSoloSeccionesYFormatosConocidos(string sections, string format, bool expected)
    {
        var result = new LogRecordExportDtoValidator().Validate(new LogRecordExportDto(sections, format));

        Assert.Equal(expected, result.IsValid);
    }
}
