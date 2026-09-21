using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.Persons;

public class PersonsEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PersonsEndpointTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void UsarToken(string rol) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CrearToken("Test", "test@mail.com", rol, Guid.NewGuid()));

    private async Task<Guid> CrearPersonaAsync()
    {
        UsarToken(RoleNames.Referente);
        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["personId"];
    }

    private async Task<Guid> RegistrarUsuarioAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { firstName = "Maria", lastName = "Gomez", email = $"{Guid.NewGuid()}@mail.com", password = "password123" });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["id"];
    }

    private async Task SeedEvaluacionAsync(Guid personId, Guid registeredById, bool isValid)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        db.PsychiatricEvaluations.Add(new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Date = DateTime.UtcNow,
            Professional = "Dra. Gomez",
            Diagnosis = "Sin diagnóstico",
            IsValid = isValid,
            RegisteredByUserId = registeredById
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateType_ResidenteSinEvaluacion_Devuelve400()
    {
        var personId = await CrearPersonaAsync();
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/type", new { personType = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateType_ResidenteConEvaluacionVigente_Devuelve204()
    {
        var personId = await CrearPersonaAsync();
        var userId = await RegistrarUsuarioAsync();
        await SeedEvaluacionAsync(personId, userId, isValid: true);
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/type", new { personType = 1 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateType_ResidenteConEvaluacionNoVigente_Devuelve400()
    {
        var personId = await CrearPersonaAsync();
        var userId = await RegistrarUsuarioAsync();
        await SeedEvaluacionAsync(personId, userId, isValid: false);
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/type", new { personType = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateType_ResidenteConEvaluacionVigente_CreaEstadiaCasona()
    {
        var personId = await CrearPersonaAsync();
        var userId = await RegistrarUsuarioAsync();
        await SeedEvaluacionAsync(personId, userId, isValid: true);
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/type", new { personType = 1 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var estadia = await db.CasonaStays.SingleOrDefaultAsync(s => s.PersonId == personId);
        Assert.NotNull(estadia);
        Assert.True(estadia!.EntryDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateType_AmbulatorioSinEvaluacion_Devuelve204()
    {
        var personId = await CrearPersonaAsync();
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/type", new { personType = 0 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateType_PersonaInexistente_Devuelve404()
    {
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/persons/{Guid.NewGuid()}/type", new { personType = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateType_TipoInvalido_Devuelve400()
    {
        var personId = await CrearPersonaAsync();
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/type", new { personType = 99 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateType_SinToken_Devuelve401()
    {
        var response = await _client.PutAsJsonAsync($"/api/persons/{Guid.NewGuid()}/type", new { personType = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateType_ComoEscucha_Devuelve403()
    {
        var personId = await CrearPersonaAsync();
        UsarToken(RoleNames.Escucha);

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/type", new { personType = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task UpdateEstado_AmbulatorioActivo_Devuelve204()
    {
        var personId = await CrearPersonaAsync();
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/personas/{personId}/estado", new { status = 0 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEstado_AmbulatorioInactivo_Devuelve204()
    {
        var personId = await CrearPersonaAsync();
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/personas/{personId}/estado", new { status = 1 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEstado_ResidenteSinEvaluacion_Devuelve400()
    {
        var personId = await CrearPersonaAsync();
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/personas/{personId}/estado", new { status = 2 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEstado_ResidenteConEvaluacionVigente_Devuelve204()
    {
        var personId = await CrearPersonaAsync();
        var userId = await RegistrarUsuarioAsync();
        await SeedEvaluacionAsync(personId, userId, isValid: true);
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/personas/{personId}/estado", new { status = 2 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEstado_PersonaInexistente_Devuelve404()
    {
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/personas/{Guid.NewGuid()}/estado", new { status = 0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEstado_SinToken_Devuelve401()
    {
        var response = await _client.PutAsJsonAsync($"/api/personas/{Guid.NewGuid()}/estado", new { status = 0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEstado_ComoEscucha_Devuelve403()
    {
        var personId = await CrearPersonaAsync();
        UsarToken(RoleNames.Escucha);

        var response = await _client.PutAsJsonAsync($"/api/personas/{personId}/estado", new { status = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}