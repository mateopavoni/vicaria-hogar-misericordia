using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Notifications;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Notifications;

public class StaleRecordAlertServiceTests
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

    private static async Task<SocialRecord> SeedRecordAsync(VicariaDbContext db, int createdDaysAgo, int? lastObservationDaysAgo = null)
    {
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Juan", LastName = "Perez" };
        db.People.Add(person);
        var record = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-createdDaysAgo),
            UpdatedAt = DateTime.UtcNow.AddDays(-createdDaysAgo)
        };
        db.SocialRecords.Add(record);
        if (lastObservationDaysAgo is not null)
        {
            db.Observations.Add(new Observation
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                Content = "x",
                CreatedAt = DateTime.UtcNow.AddDays(-lastObservationDaysAgo.Value)
            });
        }
        await db.SaveChangesAsync();
        return record;
    }

    [Fact]
    public async Task CheckAndNotifyAsync_FichaSinObservacionesHace31Dias_CreaAlertaParaReferente()
    {
        using var db = CreateDbContext();
        var record = await SeedRecordAsync(db, createdDaysAgo: 90, lastObservationDaysAgo: 31);

        var created = await new StaleRecordAlertService(db).CheckAndNotifyAsync();

        Assert.Equal(1, created);
        var n = await db.Notifications.SingleAsync();
        Assert.Equal(RoleNames.Referent, n.TargetRole);
        Assert.Equal($"/dashboard/fichas/{record.Id}", n.LinkUrl);
        Assert.Equal(StaleRecordAlertService.EventType, n.EventType);
    }

    [Fact]
    public async Task CheckAndNotifyAsync_ObservacionReciente_NoCreaAlerta()
    {
        using var db = CreateDbContext();
        await SeedRecordAsync(db, createdDaysAgo: 90, lastObservationDaysAgo: 5);

        Assert.Equal(0, await new StaleRecordAlertService(db).CheckAndNotifyAsync());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_FichaNuevaSinObservaciones_NoCreaAlerta()
    {
        using var db = CreateDbContext();
        await SeedRecordAsync(db, createdDaysAgo: 3);

        Assert.Equal(0, await new StaleRecordAlertService(db).CheckAndNotifyAsync());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_EjecutadoDosVeces_NoDuplicaLaAlerta()
    {
        using var db = CreateDbContext();
        await SeedRecordAsync(db, createdDaysAgo: 90);
        var service = new StaleRecordAlertService(db);

        await service.CheckAndNotifyAsync();
        var second = await service.CheckAndNotifyAsync();

        Assert.Equal(0, second);
        Assert.Equal(1, await db.Notifications.CountAsync());
    }
}
