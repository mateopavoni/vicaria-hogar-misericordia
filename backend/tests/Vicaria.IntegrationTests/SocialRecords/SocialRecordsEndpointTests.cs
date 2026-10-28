using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Common;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.SocialRecords;

public class SocialRecordsEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly VicariaWebApplicationFactory _factory;

    public SocialRecordsEndpointTests(VicariaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    // el token ahora se valida contra un usuario real en la base (chequeo de sesion activa)
    private async Task<Guid> SembrarActorAsync(string rol)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var roleId = db.Roles.First(r => r.Name == rol).Id;

        var actor = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Actor",
            LastName = "Test",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "x",
            Status = UserStatus.Active,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(actor);
        await db.SaveChangesAsync();
        return actor.Id;
    }

    private async Task UsarTokenAsync(string rol)
    {
        var actorId = await SembrarActorAsync(rol);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CreateToken("Test", "test@mail.com", rol, actorId));
    }

    [Fact]
    public async Task Create_ComoReferenteConSoloNombre_Devuelve201()
    {
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ComoEscucha_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Listener);

        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_SinNombre_Devuelve400()
    {
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_SinToken_Devuelve401()
    {
        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_ComoEscucha_Devuelve200()
    {
        await UsarTokenAsync(RoleNames.Referent);
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ramón", lastName = "Gómez" });
        await UsarTokenAsync(RoleNames.Listener);

        var response = await _client.GetAsync("/api/social-records?q=gomez");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultados = await response.Content.ReadFromJsonAsync<List<SocialRecordSearchResultDto>>();
        Assert.Single(resultados!);
    }

    [Fact]
    public async Task Search_SinToken_Devuelve401()
    {
        var response = await _client.GetAsync("/api/social-records?q=ana");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_DevuelveListadoPaginado()
    {
        await UsarTokenAsync(RoleNames.Referent);
        var nombreUnico = $"Valentina{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreUnico, lastName = "Ríos" });

        var response = await _client.GetAsync($"/api/social-records/list?page=1&search={nombreUnico}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<SocialRecordListItemDto>>();
        Assert.Equal(1, resultado!.Total);
        Assert.Single(resultado.Items);
    }

    [Fact]
    public async Task GetPaged_ComoDirectoraDeCasona_SoloTraeResidentes()
    {
        await UsarTokenAsync(RoleNames.Referent);
        var nombreUnico = $"Nicolas{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreUnico });

        await UsarTokenAsync(RoleNames.CasaConvivenciaDirector);
        var response = await _client.GetAsync($"/api/social-records/list?search={nombreUnico}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<SocialRecordListItemDto>>();
        Assert.Empty(resultado!.Items);
    }

    [Fact]
    public async Task GetPaged_ConFiltroEstado_DevuelveSoloEsasFichas()
    {
        await UsarTokenAsync(RoleNames.Referent);
        var nombreActiva = $"Carla{Guid.NewGuid():N}";
        var nombreInactiva = $"Diego{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreActiva });
        var creada = await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreInactiva });
        var id = (await creada.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            var ficha = await db.SocialRecords.FindAsync(id);
            ficha!.Status = SocialRecordStatus.Inactive;
            await db.SaveChangesAsync();
        }

        var inactivas = await _client.GetAsync($"/api/social-records/list?status=Inactive&search={nombreInactiva}");
        Assert.Equal(HttpStatusCode.OK, inactivas.StatusCode);
        var resultadoInactivas = await inactivas.Content.ReadFromJsonAsync<PagedResult<SocialRecordListItemDto>>();
        Assert.Single(resultadoInactivas!.Items);
        Assert.Equal(nombreInactiva, resultadoInactivas.Items[0].FirstName);

        var activas = await _client.GetAsync($"/api/social-records/list?status=Active&search={nombreActiva}");
        Assert.Equal(HttpStatusCode.OK, activas.StatusCode);
        var resultadoActivas = await activas.Content.ReadFromJsonAsync<PagedResult<SocialRecordListItemDto>>();
        Assert.Equal(1, resultadoActivas!.Total);
        Assert.Equal(nombreActiva, resultadoActivas.Items[0].FirstName);
    }

    [Fact]
    public async Task GetPaged_ConFiltroTipoPersona_DevuelveSoloEseTipo()
    {
        await UsarTokenAsync(RoleNames.Referent);
        var nombreAmbulatorio = $"Elena{Guid.NewGuid():N}";
        var nombreResidente = $"Fabio{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreAmbulatorio });
        var creada = await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreResidente });
        var id = (await creada.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            var ficha = await db.SocialRecords.FindAsync(id);
            ficha!.PersonType = PersonType.Resident;
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/social-records/list?personType=Resident");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<SocialRecordListItemDto>>();
        Assert.Single(resultado!.Items);
        Assert.Equal(nombreResidente, resultado.Items[0].FirstName);
    }

    [Fact]
    public async Task GetPaged_CombinaEstadoTipoYFecha_DevuelveSoloLaCoincidencia()
    {
        await UsarTokenAsync(RoleNames.Referent);
        var nombreActivo = $"Gaston{Guid.NewGuid():N}";
        var nombreInactivo = $"Hector{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreActivo, entryDate = DateTime.UtcNow.AddDays(-2) });
        var creada = await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreInactivo, entryDate = DateTime.UtcNow.AddDays(-2) });
        var id = (await creada.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            var ficha = await db.SocialRecords.FindAsync(id);
            ficha!.Status = SocialRecordStatus.Inactive;
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync(
            $"/api/social-records/list?status=Active&personType=Resident&entryDateFrom={DateTime.UtcNow.AddDays(-5):yyyy-MM-dd}&entryDateTo={DateTime.UtcNow.AddDays(1):yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<SocialRecordListItemDto>>();
        Assert.Empty(resultado!.Items);
    }

    [Fact]
    public async Task Update_ComoReferente_Devuelve204()
    {
        await UsarTokenAsync(RoleNames.Referent);
        var creada = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });
        var id = (await creada.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        var response = await _client.PutAsJsonAsync($"/api/social-records/{id}", new { firstName = "Ana", lastName = "Torres", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_ComoEscucha_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Listener);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ComoCoordinador_Devuelve403()
    {
        // SCRUM-117: solo Referente y Directora pueden editar, a diferencia de crear
        await UsarTokenAsync(RoleNames.CasaConvivenciaCoordinator);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ConFichaInexistente_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_SinNombre_Devuelve400()
    {
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_SinToken_Devuelve401()
    {
        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record CreatedResponse(Guid PersonId, Guid Id);

    // SCRUM-275: campos del informe de Cáritas de la ficha
    private async Task<(Guid RecordId, Guid GenderId)> SembrarFichaYGeneroAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Caritas", LastName = "Test" };
        var record = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            PersonType = PersonType.Ambulatory,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var gender = new CaritasCatalogValue
        {
            Id = Guid.NewGuid(),
            Category = CaritasCatalogCategory.Gender,
            Name = $"Genero {Guid.NewGuid():N}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.People.Add(person);
        db.SocialRecords.Add(record);
        db.CaritasCatalogValues.Add(gender);
        await db.SaveChangesAsync();
        return (record.Id, gender.Id);
    }

    [Fact]
    public async Task UpdateReportFields_ComoReferente_LosGuardaYSeVenEnElDetalle()
    {
        var (recordId, genderId) = await SembrarFichaYGeneroAsync();
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{recordId}/report-fields", new { genderId });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var detail = await _client.GetFromJsonAsync<SocialRecordDetailDto>($"/api/social-records/{recordId}");
        Assert.Equal(genderId, detail!.ReportFields.GenderId);
    }

    [Fact]
    public async Task UpdateReportFields_ComoEscucha_Devuelve403()
    {
        var (recordId, genderId) = await SembrarFichaYGeneroAsync();
        await UsarTokenAsync(RoleNames.Listener);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{recordId}/report-fields", new { genderId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateReportFields_ConValorDeOtroCampo_Devuelve400()
    {
        var (recordId, genderId) = await SembrarFichaYGeneroAsync();
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{recordId}/report-fields", new { attendanceReasonId = genderId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // SCRUM-23: auditoría de la exportación de la ficha
    [Fact]
    public async Task LogExport_ComoReferente_RegistraAuditLog()
    {
        var (recordId, _) = await SembrarFichaYGeneroAsync();
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync($"/api/social-records/{recordId}/exports", new { sections = "todo", format = "pdf" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        Assert.True(await db.AuditLogs.AnyAsync(a => a.AffectedEntity == $"SocialRecord:{recordId}" && a.Action.StartsWith("Ficha exportada")));
    }

    [Fact]
    public async Task LogExport_ComoEscucha_Devuelve403()
    {
        var (recordId, _) = await SembrarFichaYGeneroAsync();
        await UsarTokenAsync(RoleNames.Listener);

        var response = await _client.PostAsJsonAsync($"/api/social-records/{recordId}/exports", new { sections = "todo", format = "pdf" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LogExport_ConSeccionesInvalidas_Devuelve400()
    {
        var (recordId, _) = await SembrarFichaYGeneroAsync();
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync($"/api/social-records/{recordId}/exports", new { sections = "otra", format = "pdf" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // SCRUM-252: exportación de la ficha a PDF desde el backend
    [Fact]
    public async Task ExportPdf_ComoReferente_DevuelvePdfYRegistraAuditoria()
    {
        var (recordId, _) = await SembrarFichaYGeneroAsync();
        Guid personId;
        using (var scope = _factory.Services.CreateScope())
        {
            personId = scope.ServiceProvider.GetRequiredService<VicariaDbContext>().SocialRecords.Single(r => r.Id == recordId).PersonId;
        }
        await UsarTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync($"/api/persons/{personId}/export-pdf?sections=datos-observaciones");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        using var verify = _factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<VicariaDbContext>();
        Assert.True(await db.AuditLogs.AnyAsync(a => a.AffectedEntity == $"Person:{personId}" && a.Action.Contains("datos y observaciones")));
    }

    [Fact]
    public async Task ExportPdf_ComoEscucha_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Listener);

        var response = await _client.GetAsync($"/api/persons/{Guid.NewGuid()}/export-pdf?sections=datos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExportPdf_SeccionesInvalidas_Devuelve400_YPersonaInexistente_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.Referent);

        var invalid = await _client.GetAsync($"/api/persons/{Guid.NewGuid()}/export-pdf?sections=otra");
        var missing = await _client.GetAsync($"/api/persons/{Guid.NewGuid()}/export-pdf?sections=todo");

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // SCRUM-273: campos del informe pendientes
    [Fact]
    public async Task GetPendingReportFields_DevuelveLosQueFaltan()
    {
        var (recordId, genderId) = await SembrarFichaYGeneroAsync();
        Guid personId;
        using (var scope = _factory.Services.CreateScope())
        {
            personId = scope.ServiceProvider.GetRequiredService<VicariaDbContext>().SocialRecords.Single(r => r.Id == recordId).PersonId;
        }
        await UsarTokenAsync(RoleNames.Referent);
        await _client.PutAsJsonAsync($"/api/social-records/{recordId}/report-fields",
            new { genderId, housingSituationId = (Guid?)null, originLocalityId = (Guid?)null, documentationStatusId = (Guid?)null, attendanceReasonId = (Guid?)null });

        var response = await _client.GetAsync($"/api/personas/{personId}/campos-informe-pendientes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pending = await response.Content.ReadFromJsonAsync<List<PendingReportFieldDto>>();
        Assert.Equal(["housing", "origin", "document", "mainReason"], pending!.Select(p => p.Field));
    }

    [Fact]
    public async Task GetPendingReportFields_ComoEscucha_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Listener);

        var response = await _client.GetAsync($"/api/persons/{Guid.NewGuid()}/report-fields/pending");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
