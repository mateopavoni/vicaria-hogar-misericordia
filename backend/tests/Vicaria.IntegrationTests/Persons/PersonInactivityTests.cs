using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Persons;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.Persons;

public class PersonInactivityTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;

    public PersonInactivityTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CheckAndProcessInactivity_PersonaInactivaMasDe30Dias_PasaAInactivoConAuditLog()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IPersonInactivityService>();

        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@mail.com", FirstName = "Admin", LastName = "User", PasswordHash = "hash", Status = UserStatus.Active, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);

        var personOld = new Person { Id = Guid.NewGuid(), FirstName = "Inactivo 31 dias", CreatedAt = DateTime.UtcNow.AddDays(-35) };
        db.People.Add(personOld);

        var recordOld = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personOld.Id,
            Status = SocialRecordStatus.Active,
            PersonType = PersonType.Ambulatory,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-35),
            UpdatedAt = DateTime.UtcNow.AddDays(-31)
        };
        db.SocialRecords.Add(recordOld);

        var personRecent = new Person { Id = Guid.NewGuid(), FirstName = "Activo reciente", CreatedAt = DateTime.UtcNow.AddDays(-10) };
        db.People.Add(personRecent);

        var recordRecent = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = personRecent.Id,
            Status = SocialRecordStatus.Active,
            PersonType = PersonType.Ambulatory,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
        db.SocialRecords.Add(recordRecent);

        await db.SaveChangesAsync();

        await service.CheckAndProcessInactivityAsync();

        var updatedRecordOld = await db.SocialRecords.AsNoTracking().FirstAsync(r => r.Id == recordOld.Id);
        Assert.Equal(SocialRecordStatus.Inactive, updatedRecordOld.Status);

        var updatedRecordRecent = await db.SocialRecords.AsNoTracking().FirstAsync(r => r.Id == recordRecent.Id);
        Assert.Equal(SocialRecordStatus.Active, updatedRecordRecent.Status);

        var audit = await db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.AffectedEntity == $"Person:{personOld.Id}");
        Assert.NotNull(audit);
        Assert.Equal("Paso automático a inactivo por 30 días sin actividad", audit.Action);
    }
}