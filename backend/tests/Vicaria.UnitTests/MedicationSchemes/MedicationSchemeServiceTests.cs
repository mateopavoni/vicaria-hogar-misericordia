using Microsoft.EntityFrameworkCore;
using Vicaria.Application.MedicationSchemes;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.MedicationSchemes;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.MedicationSchemes;

public class MedicationSchemeServiceTests
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

    private static async Task<Guid> SeedUserAsync(VicariaDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Actor",
            LastName = "Test",
            Email = $"{Guid.NewGuid()}@mail.com",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Guid> SeedPersonAsync(
        VicariaDbContext db,
        Guid createdByUserId,
        PersonType personType = PersonType.Resident,
        bool validEvaluation = true)
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Carla",
            LastName = "Residente",
            CreatedAt = DateTime.UtcNow
        };
        db.People.Add(person);

        db.SocialRecords.Add(new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            PersonType = personType,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.PsychiatricEvaluations.Add(new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Date = DateTime.UtcNow,
            Professional = "Dra. Test",
            IsValid = validEvaluation,
            RegisteredByUserId = createdByUserId
        });

        await db.SaveChangesAsync();
        return person.Id;
    }

    private static async Task<Guid> SeedMedicationAsync(VicariaDbContext db, int stock = 10)
    {
        var medication = new MedicationCatalog
        {
            Id = Guid.NewGuid(),
            Name = $"Medicamento {Guid.NewGuid()}",
            Stock = stock
        };
        db.MedicationCatalogs.Add(medication);
        await db.SaveChangesAsync();
        return medication.Id;
    }

    private static CreateMedicationSchemeDto NewSchemeDto(Guid medicationId) =>
        new(medicationId, "  500 mg  ", "08:30");

    [Fact]
    public async Task CreateAsync_WithValidDto_PersistsSchemeWithActorAndFullData()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var medicationId = await SeedMedicationAsync(db);
        var service = new MedicationSchemeService(db);

        var result = await service.CreateAsync(personId, NewSchemeDto(medicationId), actorId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Null(result.Error);
        var scheme = Assert.IsType<MedicationSchemeDto>(result.Scheme);
        Assert.Equal(personId, scheme.PersonId);
        Assert.Equal(medicationId, scheme.MedicationId);
        Assert.Equal("500 mg", scheme.Dose);
        Assert.Equal("08:30", scheme.Time);
        Assert.True(scheme.IsActive);
        Assert.False(scheme.PendingStock);
        Assert.Equal("Actor Test", scheme.LoadedByName);
        Assert.True((DateTime.UtcNow - scheme.LoadedAt).TotalMinutes < 5);

        var persisted = await db.MedicationSchemes.SingleAsync();
        Assert.Equal(scheme.Id, persisted.Id);
        Assert.Equal(new TimeSpan(8, 30, 0), persisted.Time);

        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal(actorId, audit.UserId);
        Assert.Equal($"MedicationScheme:{scheme.Id}", audit.AffectedEntity);
    }

    [Fact]
    public async Task CreateAsync_WithZeroStock_MarksPendingStock()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var medicationId = await SeedMedicationAsync(db, stock: 0);
        var service = new MedicationSchemeService(db);

        var result = await service.CreateAsync(personId, NewSchemeDto(medicationId), actorId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Scheme!.PendingStock);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownPerson_ReturnsPersonNotFound()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var medicationId = await SeedMedicationAsync(db);
        var service = new MedicationSchemeService(db);

        var result = await service.CreateAsync(Guid.NewGuid(), NewSchemeDto(medicationId), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateMedicationSchemeError.PersonNotFound, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithAmbulatoryPerson_ReturnsPersonNotResident()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId, personType: PersonType.Ambulatory);
        var medicationId = await SeedMedicationAsync(db);
        var service = new MedicationSchemeService(db);

        var result = await service.CreateAsync(personId, NewSchemeDto(medicationId), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateMedicationSchemeError.PersonNotResident, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithoutValidEvaluation_ReturnsMissingPsychiatricEvaluation()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId, validEvaluation: false);
        var medicationId = await SeedMedicationAsync(db);
        var service = new MedicationSchemeService(db);

        var result = await service.CreateAsync(personId, NewSchemeDto(medicationId), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateMedicationSchemeError.MissingPsychiatricEvaluation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownMedication_ReturnsMedicationNotFound()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var service = new MedicationSchemeService(db);

        var result = await service.CreateAsync(personId, NewSchemeDto(Guid.NewGuid()), actorId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CreateMedicationSchemeError.MedicationNotFound, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateActiveScheme_ReturnsDuplicateScheme()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var medicationId = await SeedMedicationAsync(db);
        var service = new MedicationSchemeService(db);

        var first = await service.CreateAsync(personId, NewSchemeDto(medicationId), actorId, CancellationToken.None);
        Assert.True(first.Success);

        var second = await service.CreateAsync(personId, NewSchemeDto(medicationId), actorId, CancellationToken.None);

        Assert.False(second.Success);
        Assert.Equal(CreateMedicationSchemeError.DuplicateScheme, second.Error);
    }

    [Fact]
    public async Task CreateAsync_WithHistoricalScheme_AllowsReload()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var personId = await SeedPersonAsync(db, actorId);
        var medicationId = await SeedMedicationAsync(db);
        db.MedicationSchemes.Add(new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            MedicationId = medicationId,
            Dose = "500 mg",
            Time = new TimeSpan(8, 30, 0),
            LoadedAt = DateTime.UtcNow.AddDays(-10),
            LoadedByUserId = actorId,
            IsActive = false,
            PendingStock = false
        });
        await db.SaveChangesAsync();
        var service = new MedicationSchemeService(db);

        var result = await service.CreateAsync(personId, NewSchemeDto(medicationId), actorId, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public void Validator_AcceptsValidDto()
    {
        var validator = new CreateMedicationSchemeDtoValidator();

        var result = validator.Validate(new CreateMedicationSchemeDto(Guid.NewGuid(), "500 mg", "08:30"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "08:30")]
    [InlineData("500 mg", "8:30")]
    [InlineData("500 mg", "24:00")]
    [InlineData("500 mg", "08-30")]
    public void Validator_RejectsInvalidDto(string dose, string time)
    {
        var validator = new CreateMedicationSchemeDtoValidator();

        var result = validator.Validate(new CreateMedicationSchemeDto(Guid.NewGuid(), dose, time));

        Assert.False(result.IsValid);
    }
}
