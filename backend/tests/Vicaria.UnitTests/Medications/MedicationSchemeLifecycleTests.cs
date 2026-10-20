using Microsoft.EntityFrameworkCore;
using Vicaria.Application.MedicationSchemes;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.MedicationSchemes;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Medications;

public class MedicationSchemeLifecycleTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task GetByPersonIdAsync_WhenPersonExists_ReturnsAllSchemesOrdered()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationSchemeService(context);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Carlos" };
        var medication = new MedicationCatalog { Id = Guid.NewGuid(), Name = "Haloperidol", Stock = 10 };
        var user = new User { Id = Guid.NewGuid(), FirstName = "Ana", LastName = "Referente" };

        var activeScheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "5mg",
            Time = new TimeSpan(8, 0, 0),
            LoadedAt = DateTime.UtcNow,
            LoadedByUserId = user.Id,
            IsActive = true
        };

        var inactiveScheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "2.5mg",
            Time = new TimeSpan(20, 0, 0),
            LoadedAt = DateTime.UtcNow.AddMonths(-1),
            LoadedByUserId = user.Id,
            IsActive = false
        };

        context.People.Add(person);
        context.MedicationCatalogs.Add(medication);
        context.Users.Add(user);
        context.MedicationSchemes.AddRange(activeScheme, inactiveScheme);
        await context.SaveChangesAsync();

        var result = await service.GetByPersonIdAsync(person.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Schemes);
        Assert.Equal(2, result.Schemes.Count);
        Assert.True(result.Schemes[0].IsActive);
        Assert.False(result.Schemes[1].IsActive);
    }

    [Fact]
    public async Task GetByPersonIdAsync_WhenOnlyActiveFilterIsTrue_ReturnsOnlyActiveSchemes()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationSchemeService(context);

        var user = new User { Id = Guid.NewGuid(), FirstName = "Ana", LastName = "Referente" };
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Marta" };
        var medication = new MedicationCatalog { Id = Guid.NewGuid(), Name = "Clonazepam", Stock = 10 };

        var activeScheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "0.5mg",
            Time = new TimeSpan(21, 0, 0),
            LoadedAt = DateTime.UtcNow,
            LoadedByUserId = user.Id,
            IsActive = true
        };

        var inactiveScheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = medication.Id,
            Dose = "1mg",
            Time = new TimeSpan(21, 0, 0),
            LoadedAt = DateTime.UtcNow.AddMonths(-2),
            LoadedByUserId = user.Id,
            IsActive = false
        };

        context.Users.Add(user);
        context.People.Add(person);
        context.MedicationCatalogs.Add(medication);
        context.MedicationSchemes.AddRange(activeScheme, inactiveScheme);
        await context.SaveChangesAsync();

        var result = await service.GetByPersonIdAsync(person.Id, onlyActive: true);

        Assert.True(result.Success);
        Assert.NotNull(result.Schemes);
        Assert.Single(result.Schemes);
        Assert.True(result.Schemes[0].IsActive);
        Assert.Equal("0.5mg", result.Schemes[0].Dose);
    }

    [Fact]
    public async Task DeactivateAsync_WhenActive_SetsIsActiveFalseAndCreatesAuditLog()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationSchemeService(context);

        var actorId = Guid.NewGuid();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Esteban" };
        var scheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = Guid.NewGuid(),
            Dose = "10mg",
            Time = new TimeSpan(9, 0, 0),
            LoadedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.People.Add(person);
        context.MedicationSchemes.Add(scheme);
        await context.SaveChangesAsync();

        var result = await service.DeactivateAsync(person.Id, scheme.Id, actorId);

        Assert.True(result.Success);

        var updated = await context.MedicationSchemes.FindAsync(scheme.Id);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);

        var auditLog = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Esquema de medicación desactivado");
        Assert.NotNull(auditLog);
        Assert.Equal(actorId, auditLog.UserId);
        Assert.Equal($"MedicationScheme:{scheme.Id}", auditLog.AffectedEntity);
    }

    [Fact]
    public async Task DeactivateAsync_WhenAlreadyInactive_ReturnsConflictAlreadyInactive()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationSchemeService(context);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Esteban" };
        var scheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            MedicationId = Guid.NewGuid(),
            Dose = "10mg",
            Time = new TimeSpan(9, 0, 0),
            LoadedAt = DateTime.UtcNow,
            IsActive = false
        };

        context.People.Add(person);
        context.MedicationSchemes.Add(scheme);
        await context.SaveChangesAsync();

        var result = await service.DeactivateAsync(person.Id, scheme.Id, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(DeactivateMedicationSchemeError.AlreadyInactive, result.Error);
    }

    [Fact]
    public async Task DeactivateAsync_WhenSchemeNotFound_ReturnsSchemeNotFound()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationSchemeService(context);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Esteban" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var result = await service.DeactivateAsync(person.Id, Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(DeactivateMedicationSchemeError.SchemeNotFound, result.Error);
    }
}