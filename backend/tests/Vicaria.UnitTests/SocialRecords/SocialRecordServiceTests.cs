using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Persons;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.SocialRecords;

namespace Vicaria.UnitTests.SocialRecords;

public class SocialRecordServiceTests
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

    [Fact]
    public async Task CreateAsync_ConSoloElNombre_CreaPersonaYFichaActiva()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null);

        var resultado = await service.CreateAsync(dto, Guid.NewGuid());

        Assert.True(resultado.Success);
        var persona = await db.People.FindAsync(resultado.PersonId);
        Assert.Equal("Ana", persona!.FirstName);
        var ficha = await db.SocialRecords.FindAsync(resultado.SocialRecordId);
        Assert.Equal(SocialRecordStatus.Active, ficha!.Status);
    }

    [Fact]
    public async Task CreateAsync_ConContacto_LoVinculaALaFicha()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var contacto = new ContactDto("Juan", "Perez", "1234", "Calle Falsa 123");
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, contacto);

        var resultado = await service.CreateAsync(dto, Guid.NewGuid());

        var contactoGuardado = await db.Contacts.FirstOrDefaultAsync(c => c.SocialRecordId == resultado.SocialRecordId);
        Assert.NotNull(contactoGuardado);
        Assert.Equal("Juan", contactoGuardado!.FirstName);
    }

    [Fact]
    public async Task CreateAsync_SinContacto_NoCreaContacto()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null);

        var resultado = await service.CreateAsync(dto, Guid.NewGuid());

        var hayContacto = await db.Contacts.AnyAsync(c => c.SocialRecordId == resultado.SocialRecordId);
        Assert.False(hayContacto);
    }

    [Fact]
    public async Task CreateAsync_RegistraAuditLogConElActor()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var actorId = Guid.NewGuid();
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null);

        var resultado = await service.CreateAsync(dto, actorId);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.AffectedEntity == $"SocialRecord:{resultado.SocialRecordId}");
        Assert.NotNull(log);
        Assert.Equal(actorId, log!.UserId);
    }

    [Fact]
    public async Task SearchAsync_ConTextoParcial_EncuentraLaFicha()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ramón", "Gómez", null, null, null, null, null, null, null, null, null, false, null, null);
        await service.CreateAsync(dto, Guid.NewGuid());

        var resultados = await service.SearchAsync("gom");

        Assert.Single(resultados);
        Assert.Equal("Ramón Gómez", resultados[0].FullName);
    }

    [Fact]
    public async Task SearchAsync_IgnoraTildesYMayusculas()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Joaquín", "Gómez", null, null, null, null, null, null, null, null, null, false, null, null);
        await service.CreateAsync(dto, Guid.NewGuid());

        
        var resultadosSinTilde = await service.SearchAsync("Joaquin");
        Assert.Single(resultadosSinTilde);

        
        var resultadosMayusculas = await service.SearchAsync("JOAQUÍN");
        Assert.Single(resultadosMayusculas);
    }

    [Fact]
    public async Task SearchAsync_SinCoincidencias_DevuelveListaVacia()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null);
        await service.CreateAsync(dto, Guid.NewGuid());

        var resultados = await service.SearchAsync("noexiste");

        Assert.Empty(resultados);
    }

    [Fact]
    public async Task SearchAsync_ConQueryVacio_DevuelveListaVacia()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);

        var resultados = await service.SearchAsync("");

        Assert.Empty(resultados);
    }

    [Fact]
    public async Task UpdateAsync_ConFichaExistente_ActualizaLosDatos()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null), Guid.NewGuid());
        var dto = new UpdateSocialRecordDto("Ana", "Torres", "30111222", null, null, null, "Nuevo motivo", null, null, null, null, true, null, null);

        var resultado = await service.UpdateAsync(creada.SocialRecordId, dto, Guid.NewGuid());

        Assert.True(resultado.Success);
        var persona = await db.People.FindAsync(creada.PersonId);
        Assert.Equal("Torres", persona!.LastName);
        var ficha = await db.SocialRecords.FindAsync(creada.SocialRecordId);
        Assert.Equal("Nuevo motivo", ficha!.ReasonForEntry);
    }

    [Fact]
    public async Task UpdateAsync_ConFichaInexistente_DevuelveNotFound()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null);

        var resultado = await service.UpdateAsync(Guid.NewGuid(), dto, Guid.NewGuid());

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task UpdateAsync_RegistraAuditLogConElActor()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null), Guid.NewGuid());
        var actorId = Guid.NewGuid();
        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null);

        await service.UpdateAsync(creada.SocialRecordId, dto, actorId);

        var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.AffectedEntity == $"SocialRecord:{creada.SocialRecordId}" && a.UserId == actorId);
        Assert.NotNull(log);
    }
    [Fact]
    public async Task CountByFilterAsync_FiltraCorrectamentePorDniYFecha()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var actorId = Guid.NewGuid();

        //Con DNI y fecha de ingreso reciente
        var dto1 = new CreateSocialRecordDto("Marcos", "Paz", "35111222", null, null, null, null, DateTime.UtcNow.AddDays(-5), null, null, null, false, null, null);
        await service.CreateAsync(dto1, actorId);

        //Sin DNI y fecha de ingreso antigua
        var dto2 = new CreateSocialRecordDto("Lucas", "Sosa", null, null, null, null, null, DateTime.UtcNow.AddDays(-40), null, null, null, false, null, null);
        await service.CreateAsync(dto2, actorId);

        //Conteo total sin filtros
        var countTotal = await service.CountByFilterAsync(new FilterSocialRecordsDto());
        Assert.Equal(2, countTotal);

        //Solo con DNI
        var countConDni = await service.CountByFilterAsync(new FilterSocialRecordsDto(HasDni: true));
        Assert.Equal(1, countConDni);

        //Sin DNI
        var countSinDni = await service.CountByFilterAsync(new FilterSocialRecordsDto(HasDni: false));
        Assert.Equal(1, countSinDni);

        //Por rango de fecha de ingreso
        var countFecha = await service.CountByFilterAsync(new FilterSocialRecordsDto(EntryDateFrom: DateTime.UtcNow.AddDays(-10)));
        Assert.Equal(1, countFecha);
    }

    [Fact]
    public async Task GetPagedAsync_CombinaBusquedaYFiltros()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var actorId = Guid.NewGuid();

        await service.CreateAsync(new CreateSocialRecordDto("Marcos", "Paz", "35111222", null, null, null, null, null, null, null, null, false, null, null), actorId);
        await service.CreateAsync(new CreateSocialRecordDto("Lucas", "Sosa", null, null, null, null, null, null, null, null, null, false, null, null), actorId);

        // sin filtros ni busqueda: trae todo
        var todos = await service.GetPagedAsync(page: 1, search: null, filter: null);
        Assert.Equal(2, todos.Total);
        Assert.Equal(1, todos.TotalPages);

        // busqueda por nombre
        var porNombre = await service.GetPagedAsync(page: 1, search: "marcos", filter: null);
        Assert.Single(porNombre.Items);
        Assert.Equal("Marcos", porNombre.Items[0].FirstName);

        // filtro por DNI
        var conDni = await service.GetPagedAsync(page: 1, search: null, filter: new FilterSocialRecordsDto(HasDni: true));
        Assert.Single(conDni.Items);
        Assert.Equal("35111222", conDni.Items[0].Dni);
    }

    [Fact]
    public async Task GetPagedAsync_ComoDirectoraDeCasona_SoloTraeResidentes()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var actorId = Guid.NewGuid();

        var ambulatorio = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null), actorId);
        var residente = await service.CreateAsync(new CreateSocialRecordDto("Beto", null, null, null, null, null, null, null, null, null, null, false, null, null), actorId);
        var socialRecordResidente = await db.SocialRecords.FirstAsync(r => r.Id == residente.SocialRecordId);
        socialRecordResidente.PersonType = PersonType.Resident;
        await db.SaveChangesAsync();

        var result = await service.GetPagedAsync(page: 1, search: null, filter: null, personTypeFilter: PersonType.Resident);

        Assert.Single(result.Items);
        Assert.Equal(residente.PersonId, result.Items[0].PersonId);
    }

    [Fact]
    public async Task GetPagedAsync_ConEstado_FiltraPorEstado()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var actorId = Guid.NewGuid();

        await service.CreateAsync(new CreateSocialRecordDto("Marcos", null, null, null, null, null, null, null, null, null, null, false, null, null), actorId);
        var inactiva = await service.CreateAsync(new CreateSocialRecordDto("Lucas", null, null, null, null, null, null, null, null, null, null, false, null, null), actorId);
        var socialRecordInactiva = await db.SocialRecords.FirstAsync(r => r.Id == inactiva.SocialRecordId);
        socialRecordInactiva.Status = SocialRecordStatus.Inactive;
        await db.SaveChangesAsync();

        var activas = await service.GetPagedAsync(page: 1, search: null, filter: new FilterSocialRecordsDto(Status: SocialRecordStatus.Active));
        var inactivas = await service.GetPagedAsync(page: 1, search: null, filter: new FilterSocialRecordsDto(Status: SocialRecordStatus.Inactive));

        Assert.Single(activas.Items);
        Assert.Equal("Marcos", activas.Items[0].FirstName);
        Assert.Single(inactivas.Items);
        Assert.Equal("Lucas", inactivas.Items[0].FirstName);
    }

    [Fact]
    public async Task GetPagedAsync_ConTipoPersona_FiltraPorTipo()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var actorId = Guid.NewGuid();

await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, PersonType.Ambulatory, null, null, null, null, null, false, null, null), actorId);
        var residente = await service.CreateAsync(new CreateSocialRecordDto("Beto", null, null, null, null, PersonType.Resident, null, null, null, null, null, false, null, null), actorId);

        var ambulatorios = await service.GetPagedAsync(page: 1, search: null, filter: new FilterSocialRecordsDto(PersonType: PersonType.Ambulatory));
        var residentes = await service.GetPagedAsync(page: 1, search: null, filter: new FilterSocialRecordsDto(PersonType: PersonType.Resident));

        Assert.Single(ambulatorios.Items);
        Assert.Equal("Ana", ambulatorios.Items[0].FirstName);
        Assert.Single(residentes.Items);
        Assert.Equal("Beto", residentes.Items[0].FirstName);
    }

    [Fact]
    public async Task GetPagedAsync_CombinaEstadoTipoYRangoDeFechas()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var actorId = Guid.NewGuid();

        await service.CreateAsync(new CreateSocialRecordDto("Marcos", null, null, null, null, PersonType.Resident, null, DateTime.UtcNow.AddDays(-2), null, null, null, false, null, null), actorId);
        var residenteInactivo = await service.CreateAsync(new CreateSocialRecordDto("Lucas", null, null, null, null, PersonType.Resident, null, DateTime.UtcNow.AddDays(-2), null, null, null, false, null, null), actorId);
        var socialRecordInactivo = await db.SocialRecords.FirstAsync(r => r.Id == residenteInactivo.SocialRecordId);
        socialRecordInactivo.Status = SocialRecordStatus.Inactive;
        await db.SaveChangesAsync();

        var filter = new FilterSocialRecordsDto(
            EntryDateFrom: DateTime.UtcNow.AddDays(-5),
            EntryDateTo: DateTime.UtcNow,
            Status: SocialRecordStatus.Active,
            PersonType: PersonType.Resident);

        var result = await service.GetPagedAsync(page: 1, search: null, filter: filter);

        Assert.Single(result.Items);
        Assert.Equal("Marcos", result.Items[0].FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_ConFichaExistente_DevuelveElPerfilCompleto()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var contacto = new ContactDto("Juan", "Perez", "1234", "Calle Falsa 123");
        var dto = new CreateSocialRecordDto("Ana", "Gomez", "30111222", null, null, null, "Derivación", null, null, null, null, false, null, contacto);
        var creada = await service.CreateAsync(dto, Guid.NewGuid());

        var perfil = await service.GetByIdAsync(creada.SocialRecordId);

        Assert.NotNull(perfil);
        Assert.Equal("Ana", perfil!.FirstName);
        Assert.Equal("Gomez", perfil.LastName);
        Assert.NotNull(perfil.Contact);
        Assert.Equal("Juan", perfil.Contact!.FirstName);
        Assert.Empty(perfil.StaysHistory);
    }

    [Fact]
    public async Task GetByIdAsync_ConFichaInexistente_DevuelveNull()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);

        var perfil = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(perfil);
    }

    // bug reportado 2026-09-23: se guardaba el DNI con puntos/espacios y la búsqueda
    // comparaba solo dígitos, así que nunca matcheaba.
    [Fact]
    public async Task CreateAsync_ConDniConPuntosYEspacios_LoGuardaSoloConDigitos()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, "38.123.456", null, null, null, null, null, null, null, null, false, null, null);

        var creada = await service.CreateAsync(dto, Guid.NewGuid());

        var persona = await db.People.FindAsync(creada.PersonId);
        Assert.Equal("38123456", persona!.Dni);
    }

    [Fact]
    public async Task SearchAsync_ConDniSinPuntos_EncuentraUnaFichaGuardadaConPuntos()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var dto = new CreateSocialRecordDto("Ana", null, "38.123.456", null, null, null, null, null, null, null, null, false, null, null);
        await service.CreateAsync(dto, Guid.NewGuid());

        var resultados = await service.SearchAsync("38123456");

        Assert.Single(resultados);
    }

    [Fact]
    public async Task UpdateAsync_ConDniConPuntos_LoNormalizaSoloADigitos()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null), Guid.NewGuid());
        var dto = new UpdateSocialRecordDto("Ana", null, "30.999.111", null, null, null, null, null, null, null, null, false, null, null);

        await service.UpdateAsync(creada.SocialRecordId, dto, Guid.NewGuid());

        var persona = await db.People.FindAsync(creada.PersonId);
        Assert.Equal("30999111", persona!.Dni);
    }

    // bug reportado 2026-09-23: UpdateSocialRecordDto no tenía Contact, así que editar el
    // contacto de referencia se perdía en silencio
    [Fact]
    public async Task UpdateAsync_ConContactoNuevo_LoCrea()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null), Guid.NewGuid());
        var contacto = new ContactDto("Juan", "Perez", "3511112233", "Calle Falsa 123");
        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, contacto);

        await service.UpdateAsync(creada.SocialRecordId, dto, Guid.NewGuid());

        var contactoGuardado = await db.Contacts.SingleAsync(c => c.SocialRecordId == creada.SocialRecordId);
        Assert.Equal("Juan", contactoGuardado.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_ConContactoExistente_LoActualiza()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var contactoInicial = new ContactDto("Juan", "Perez", "3511112233", "Calle Falsa 123");
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, contactoInicial), Guid.NewGuid());
        var contactoNuevo = new ContactDto("Maria", "Lopez", "3519998888", "Otra calle 456");
        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, contactoNuevo);

        await service.UpdateAsync(creada.SocialRecordId, dto, Guid.NewGuid());

        var contactoGuardado = await db.Contacts.SingleAsync(c => c.SocialRecordId == creada.SocialRecordId);
        Assert.Equal("Maria", contactoGuardado.FirstName);
        Assert.Equal("Otra calle 456", contactoGuardado.Address);
    }

    [Fact]
    public async Task UpdateAsync_SinContactoYHabiaUno_LoElimina()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var contactoInicial = new ContactDto("Juan", "Perez", "3511112233", "Calle Falsa 123");
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, contactoInicial), Guid.NewGuid());
        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, null, null, null, null, null, null, false, null, null);

        await service.UpdateAsync(creada.SocialRecordId, dto, Guid.NewGuid());

        Assert.Empty(await db.Contacts.Where(c => c.SocialRecordId == creada.SocialRecordId).ToListAsync());
    }

    // bug reportado 2026-09-23: UpdateAsync (edición genérica de ficha) cambiaba
    // PersonType sin la misma validación/side-effects que UpdatePersonTypeAsync
    [Fact]
    public async Task UpdateAsync_AResidenteSinEvaluacion_DevuelveMissingPsychiatricEvaluation()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, PersonType.Ambulatory, null, null, null, null, null, false, null, null), Guid.NewGuid());
        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, PersonType.Resident, null, null, null, null, null, false, null, null);

        var resultado = await service.UpdateAsync(creada.SocialRecordId, dto, Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(UpdateSocialRecordError.MissingPsychiatricEvaluation, resultado.Error);
    }

    [Fact]
    public async Task UpdateAsync_AAmbulatorioConEstadiaActiva_DevuelveActiveStayMustBeExitedFirst()
    {
        using var db = CrearDbContext();
        var service = new SocialRecordService(db);
        var creada = await service.CreateAsync(new CreateSocialRecordDto("Ana", null, null, null, null, PersonType.Ambulatory, null, null, null, null, null, false, null, null), Guid.NewGuid());
        db.PsychiatricEvaluations.Add(new PsychiatricEvaluation { Id = Guid.NewGuid(), PersonId = creada.PersonId, Date = DateTime.UtcNow, IsValid = true });
        await db.SaveChangesAsync();
        await service.UpdatePersonTypeAsync(creada.PersonId, new UpdatePersonTypeDto(PersonType.Resident), Guid.NewGuid());

        var dto = new UpdateSocialRecordDto("Ana", null, null, null, null, PersonType.Ambulatory, null, null, null, null, null, false, null, null);
        var resultado = await service.UpdateAsync(creada.SocialRecordId, dto, Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(UpdateSocialRecordError.ActiveStayMustBeExitedFirst, resultado.Error);
    }
}
