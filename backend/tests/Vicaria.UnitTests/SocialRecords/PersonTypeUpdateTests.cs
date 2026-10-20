using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Persons;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.SocialRecords;

namespace Vicaria.UnitTests.SocialRecords;

public class PersonTypeUpdateTests
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

    private async Task<CreateSocialRecordResult> CrearPersonaConFicha(
        VicariaDbContext db,
        PersonType? personType = null)
    {
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, personType, null, null, null, null, null, false, null, null);
        return await service.CreateAsync(dto, Guid.NewGuid());
    }

    private async Task SeedEvaluation(VicariaDbContext db, Guid personId, bool isValid, DateTime? date = null)
    {
        db.PsychiatricEvaluations.Add(new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Date = date ?? DateTime.UtcNow,
            IsValid = isValid
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SetResident_SinEvaluacion_DevuelveMissingPsychiatricEvaluation()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);

        var resultado = await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(UpdatePersonTypeError.MissingPsychiatricEvaluation, resultado.Error);
    }

    [Fact]
    public async Task SetResident_ConEvaluacionVigente_DevuelveOk()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);
        await SeedEvaluation(db, creada.PersonId, isValid: true);

        var resultado = await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());

        Assert.True(resultado.Success);
        var ficha = await db.SocialRecords.FindAsync(creada.SocialRecordId);
        Assert.Equal(PersonType.Resident, ficha!.PersonType);
    }

    [Fact]
    public async Task SetResident_ConEvaluacionVigente_CreaEstadiaCasaConvivenciaConEntryDateHoy()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);
        await SeedEvaluation(db, creada.PersonId, isValid: true);
        var antes = DateTime.UtcNow.AddMinutes(-1);

        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());

        var estadia = await db.CasaConvivenciaStays.SingleOrDefaultAsync(s => s.PersonId == creada.PersonId);
        var despues = DateTime.UtcNow.AddMinutes(1);
        Assert.NotNull(estadia);
        Assert.True(estadia!.EntryDate >= antes && estadia.EntryDate <= despues);
        Assert.Null(estadia.ExitDate);
    }

    [Fact]
    public async Task SetAmbulatory_NoCreaEstadiaCasaConvivencia()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);

        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Ambulatory), Guid.NewGuid());

        Assert.Empty(await db.CasaConvivenciaStays.Where(s => s.PersonId == creada.PersonId).ToListAsync());
    }

    [Fact]
    public async Task ReesetearResidente_NoDuplicaEstadiaCasaConvivencia()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);
        await SeedEvaluation(db, creada.PersonId, isValid: true);

        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());
        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());

        Assert.Single(await db.CasaConvivenciaStays.Where(s => s.PersonId == creada.PersonId).ToListAsync());
    }

    [Fact]
    public async Task CreaEstadiaCasaConvivencia_RegistraAuditLog()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);
        await SeedEvaluation(db, creada.PersonId, isValid: true);
        var actorId = Guid.NewGuid();

        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), actorId);

        var estadia = await db.CasaConvivenciaStays.SingleAsync(s => s.PersonId == creada.PersonId);
        var log = await db.AuditLogs.FirstOrDefaultAsync(a =>
            a.AffectedEntity == $"CasaConvivenciaStay:{estadia.Id}" && a.UserId == actorId);
        Assert.NotNull(log);
    }

    [Fact]
    public async Task SetResident_ConEvaluacionNoVigente_DevuelveMissingPsychiatricEvaluation()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);
        await SeedEvaluation(db, creada.PersonId, isValid: false);

        var resultado = await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(UpdatePersonTypeError.MissingPsychiatricEvaluation, resultado.Error);
    }

    [Fact]
    public async Task SetAmbulatory_SinEvaluacion_DevuelveOk()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);

        var resultado = await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Ambulatory), Guid.NewGuid());

        Assert.True(resultado.Success);
        var ficha = await db.SocialRecords.FindAsync(creada.SocialRecordId);
        Assert.Equal(PersonType.Ambulatory, ficha!.PersonType);
    }

    [Fact]
    public async Task PersonaInexistente_DevuelvePersonNotFound()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);

        var resultado = await service.UpdatePersonTypeAsync(Guid.NewGuid(), new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(UpdatePersonTypeError.PersonNotFound, resultado.Error);
    }

    [Fact]
    public async Task PersonaSinFicha_DevuelveSocialRecordNotFound()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var person = new Person { Id = Guid.NewGuid(), FirstName = "SinFicha", CreatedAt = DateTime.UtcNow };
        db.People.Add(person);
        await db.SaveChangesAsync();

        var resultado = await service.UpdatePersonTypeAsync(person.Id, new UpdatePersonTypeDto(PersonType.Ambulatory), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(UpdatePersonTypeError.SocialRecordNotFound, resultado.Error);
    }

    [Fact]
    public async Task CambioTipo_RegistraAuditLog()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);
        var actorId = Guid.NewGuid();

        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Ambulatory), actorId);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a =>
            a.AffectedEntity == $"Person:{creada.PersonId}" && a.UserId == actorId);
        Assert.NotNull(log);
    }

    // bug reportado 2026-09-23: se podía dejar de ser Residente (por este endpoint o por
    // UpdateAsync/UpdatePersonProfileStatusAsync) con una estadía todavía abierta,
    // desincronizando el botón de ingreso/egreso del frontend.
    [Fact]
    public async Task SetAmbulatory_ConEstadiaActiva_DevuelveActiveStayMustBeExitedFirst()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);
        await SeedEvaluation(db, creada.PersonId, isValid: true);
        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());

        var resultado = await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Ambulatory), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(UpdatePersonTypeError.ActiveStayMustBeExitedFirst, resultado.Error);
        var ficha = await db.SocialRecords.FindAsync(creada.SocialRecordId);
        Assert.Equal(PersonType.Resident, ficha!.PersonType);
    }

    [Fact]
    public async Task SetAmbulatory_ConEstadiaYaCerrada_PermiteElCambio()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db);
        await SeedEvaluation(db, creada.PersonId, isValid: true);
        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());
        var estadia = await db.CasaConvivenciaStays.SingleAsync(s => s.PersonId == creada.PersonId);
        estadia.ExitDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var resultado = await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Ambulatory), Guid.NewGuid());

        Assert.True(resultado.Success);
    }

    // bug reportado 2026-09-23: el historial de tipo de persona nunca existió
    [Fact]
    public async Task CambioTipo_RegistraEntradaEnHistorial()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db, PersonType.Ambulatory);
        await SeedEvaluation(db, creada.PersonId, isValid: true);
        var actorId = Guid.NewGuid();

        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), actorId);

        var cambio = await db.PersonTypeChanges.SingleAsync(c => c.PersonId == creada.PersonId);
        Assert.Equal(PersonType.Ambulatory, cambio.PreviousType);
        Assert.Equal(PersonType.Resident, cambio.NewType);
        Assert.Equal(actorId, cambio.ChangedByUserId);
    }

    [Fact]
    public async Task CambioAlMismoTipo_NoRegistraEntradaEnHistorial()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await CrearPersonaConFicha(db, PersonType.Ambulatory);

        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Ambulatory), Guid.NewGuid());

        Assert.Empty(await db.PersonTypeChanges.Where(c => c.PersonId == creada.PersonId).ToListAsync());
    }
}