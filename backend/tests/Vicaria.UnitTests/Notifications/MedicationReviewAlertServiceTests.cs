using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Notifications;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Notifications;

public class MedicationReviewAlertServiceTests
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

    [Fact]
    public async Task CheckAndNotifyAsync_EsquemaSinRevision_LinkApuntaAlIdDeLaFichaNoAlDeLaPersona()
    {
        using var db = CreateDbContext();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Carlos", LastName = "Rodríguez" };
        var record = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            PersonType = PersonType.Resident,
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-60)
        };
        var medication = new MedicationCatalog { Id = Guid.NewGuid(), Name = "Enalapril 10 mg", Stock = 10 };
        db.People.Add(person);
        db.SocialRecords.Add(record);
        db.MedicationCatalogs.Add(medication);
        db.MedicationSchemes.Add(new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "1 comprimido",
            Time = new TimeSpan(8, 0, 0),
            LoadedAt = DateTime.UtcNow.AddDays(-45),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var created = await new MedicationReviewAlertService(db).CheckAndNotifyAsync();

        Assert.True(created > 0);
        var notifications = await db.Notifications.ToListAsync();
        Assert.NotEmpty(notifications);
        Assert.All(notifications, n => Assert.Equal($"/dashboard/fichas/{record.Id}", n.LinkUrl));
    }
}
