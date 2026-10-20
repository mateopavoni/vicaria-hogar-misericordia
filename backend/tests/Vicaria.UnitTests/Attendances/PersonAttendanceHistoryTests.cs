using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Vicaria.Api.Controllers;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Attendances;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Attendances;

public class PersonAttendanceHistoryTests
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

    private static async Task<Person> CrearPersona(VicariaDbContext db, string nombre = "Carlos")
    {
        var person = new Person { Id = Guid.NewGuid(), FirstName = nombre, CreatedAt = DateTime.UtcNow };
        db.People.Add(person);
        await db.SaveChangesAsync();
        return person;
    }

    private static async Task CrearStayActiva(VicariaDbContext db, Guid personId)
    {
        db.CasaConvivenciaStays.Add(new CasaConvivenciaStay
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            EntryDate = DateTime.UtcNow.AddMonths(-1),
            ExitDate = null
        });
        await db.SaveChangesAsync();
    }

    private static async Task CrearAsistencia(VicariaDbContext db, Guid personId, AttendanceActivity activity, DateTime date)
    {
        db.Attendances.Add(new Attendance
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Activity = activity,
            Date = date,
            CreatedByUserId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_PersonaInexistente_DevuelveNotFound()
    {
        using var db = CrearDbContext();
        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(Guid.NewGuid(), filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.Equal(GetPersonAttendanceHistoryError.NotFound, result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_DirectoraPersonaNoResidente_DevuelveForbidden()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.CasaConvivenciaDirector]);

        Assert.Equal(GetPersonAttendanceHistoryError.Forbidden, result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_DirectoraPersonaResidenteActiva_DevuelveSuccess()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        await CrearStayActiva(db, person.Id);
        await CrearAsistencia(db, person.Id, AttendanceActivity.CasaConvivencia, DateTime.UtcNow);
        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.CasaConvivenciaDirector]);

        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Groups);
        Assert.Equal(AttendanceActivity.CasaConvivencia, result.Data.Groups[0].Activity);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_ReferentePersonaNoResidente_DevuelveSuccess()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, DateTime.UtcNow);
        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Groups);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_FiltroPorActividad_RetornaSoloLaActividadSolicitada()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, DateTime.UtcNow.AddDays(-2));
        await CrearAsistencia(db, person.Id, AttendanceActivity.CasaConvivencia, DateTime.UtcNow.AddDays(-1));
        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(AttendanceActivity.DayCenter, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Groups);
        Assert.Equal(AttendanceActivity.DayCenter, result.Data.Groups[0].Activity);
        Assert.Single(result.Data.Groups[0].Attendances);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_FiltroPorRangoFechas_FiltraCorrectamente()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        var baseDate = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, baseDate.AddDays(-5));
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, baseDate);
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, baseDate.AddDays(5));
        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, baseDate.AddDays(-1), baseDate.AddDays(1));

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Groups);
        Assert.Single(result.Data.Groups[0].Attendances);
        Assert.Equal(baseDate, result.Data.Groups[0].Attendances[0].Date);
    }

    [Fact]
    public void Validator_FechaDesdeMayorAFechaHasta_FallaValidacion()
    {
        var validator = new GetPersonAttendanceHistoryValidator();
        var filter = new GetPersonAttendanceHistoryFilterDto(null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow);

        var validationResult = validator.Validate(filter);

        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.ErrorMessage == "La fecha desde no puede ser posterior a la fecha hasta.");
    }

    [Fact]
    public void Validator_FechasValidas_PasaValidacion()
    {
        var validator = new GetPersonAttendanceHistoryValidator();
        var filter = new GetPersonAttendanceHistoryFilterDto(null, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);

        var validationResult = validator.Validate(filter);

        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public void PersonAttendancesController_TieneAtributoAuthorizeConRolesCorrectos()
    {
        var controllerType = typeof(PersonAttendancesController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttribute);
        Assert.Contains(RoleNames.Referent, authorizeAttribute.Roles);
        Assert.Contains(RoleNames.CasaConvivenciaDirector, authorizeAttribute.Roles);
        Assert.Contains(RoleNames.CasaConvivenciaCoordinator, authorizeAttribute.Roles);
        Assert.DoesNotContain(RoleNames.Listener, authorizeAttribute.Roles);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_SinAsistencias_CalculaIndicadoresVacios()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.NotNull(result.Data);
        var indicators = result.Data.Indicators;
        Assert.Equal(0, indicators.TotalDiasAsistidos);
        Assert.Equal(0, indicators.RachaActual);
        Assert.Null(indicators.UltimaAsistencia);
        Assert.True(indicators.SinAsistir7Dias);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_AsistenciasConsecutivasHastaHoy_CalculaRachaYUltimaAsistencia()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        var today = DateTime.UtcNow.Date;
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, today.AddDays(-2));
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, today.AddDays(-1));
        await CrearAsistencia(db, person.Id, AttendanceActivity.CasaConvivencia, today);

        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.NotNull(result.Data);
        var indicators = result.Data.Indicators;
        Assert.Equal(3, indicators.TotalDiasAsistidos);
        Assert.Equal(3, indicators.RachaActual);
        Assert.Equal(today, indicators.UltimaAsistencia);
        Assert.False(indicators.SinAsistir7Dias);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_AsistenciaAyerPeroNoHoy_MantieneRachaViva()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        var today = DateTime.UtcNow.Date;
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, today.AddDays(-2));
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, today.AddDays(-1));

        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.NotNull(result.Data);
        var indicators = result.Data.Indicators;
        Assert.Equal(2, indicators.TotalDiasAsistidos);
        Assert.Equal(2, indicators.RachaActual);
        Assert.Equal(today.AddDays(-1), indicators.UltimaAsistencia);
        Assert.False(indicators.SinAsistir7Dias);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_AsistenciaHaceMasDe7Dias_MarcaSinAsistir7Dias()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        var pastDate = DateTime.UtcNow.Date.AddDays(-8);
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, pastDate);

        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.NotNull(result.Data);
        var indicators = result.Data.Indicators;
        Assert.Equal(1, indicators.TotalDiasAsistidos);
        Assert.Equal(0, indicators.RachaActual);
        Assert.Equal(pastDate, indicators.UltimaAsistencia);
        Assert.True(indicators.SinAsistir7Dias);
    }

    [Fact]
    public async Task GetPersonAttendanceHistoryAsync_MultiplesActividadesMismoDia_CuentaUnSoloDiaAsistido()
    {
        using var db = CrearDbContext();
        var person = await CrearPersona(db);
        var today = DateTime.UtcNow.Date;
        await CrearAsistencia(db, person.Id, AttendanceActivity.DayCenter, today);
        await CrearAsistencia(db, person.Id, AttendanceActivity.CasaConvivencia, today);

        var service = new AttendanceService(db);
        var filter = new GetPersonAttendanceHistoryFilterDto(null, null, null);

        var result = await service.GetPersonAttendanceHistoryAsync(person.Id, filter, Guid.NewGuid(), [RoleNames.Referent]);

        Assert.NotNull(result.Data);
        var indicators = result.Data.Indicators;
        Assert.Equal(1, indicators.TotalDiasAsistidos);
        Assert.Equal(1, indicators.RachaActual);
    }
}