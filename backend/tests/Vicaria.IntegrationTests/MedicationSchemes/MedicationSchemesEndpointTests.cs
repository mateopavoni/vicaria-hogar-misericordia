using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.MedicationSchemes;

public class MedicationSchemesEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MedicationSchemesEndpointTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> SeedActorAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var roleId = db.Roles.First(r => r.Name == role).Id;

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

    private async Task UseTokenAsync(string role)
    {
        var actorId = await SeedActorAsync(role);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CreateToken("Test", "test@mail.com", role, actorId));
    }

    // residente con ficha, evaluación psiquiátrica y un medicamento en catálogo
    private async Task<(Guid PersonId, Guid MedicationId)> SeedResidentAsync(
        PersonType personType = PersonType.Resident,
        bool validEvaluation = true,
        int stock = 10)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Owner",
            LastName = "Test",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "x",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Carla",
            LastName = "Residente",
            CreatedAt = DateTime.UtcNow
        };
        var medication = new MedicationCatalog
        {
            Id = Guid.NewGuid(),
            Name = $"Medicamento {Guid.NewGuid()}",
            Stock = stock
        };

        db.Users.Add(owner);
        db.People.Add(person);
        db.SocialRecords.Add(new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            PersonType = personType,
            CreatedByUserId = owner.Id,
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
            RegisteredByUserId = owner.Id
        });
        db.MedicationCatalogs.Add(medication);
        await db.SaveChangesAsync();
        return (person.Id, medication.Id);
    }

    private static object NewSchemeBody(Guid medicationId, string dose = "500 mg", string time = "08:30") =>
        new { medicationId, dose, time };

    [Fact]
    public async Task Create_ResidenteConEvaluacionVigente_Devuelve201ConEsquemaCompleto()
    {
        var (personId, medicationId) = await SeedResidentAsync();
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(medicationId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("500 mg", body.GetProperty("dose").GetString());
        Assert.Equal("08:30", body.GetProperty("time").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.False(body.GetProperty("pendingStock").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("medicationName").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("loadedByName").GetString()));
    }

    [Fact]
    public async Task Create_SinStock_MarcaPendienteStock()
    {
        var (personId, medicationId) = await SeedResidentAsync(stock: 0);
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(medicationId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("pendingStock").GetBoolean());
    }

    [Fact]
    public async Task Create_SinEvaluacionVigente_Devuelve400()
    {
        var (personId, medicationId) = await SeedResidentAsync(validEvaluation: false);
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(medicationId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MedicamentoFueraDeCatalogo_Devuelve404()
    {
        var (personId, _) = await SeedResidentAsync();
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_EsquemaDuplicadoVigente_Devuelve409()
    {
        var (personId, medicationId) = await SeedResidentAsync();
        await UseTokenAsync(RoleNames.Referent);
        var first = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(medicationId));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(medicationId));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_ConRolEscucha_Devuelve403()
    {
        var (personId, medicationId) = await SeedResidentAsync();
        await UseTokenAsync(RoleNames.Listener);

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(medicationId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_DirectoraConAmbulatorio_Devuelve404()
    {
        var (personId, medicationId) = await SeedResidentAsync(personType: PersonType.Ambulatory);
        await UseTokenAsync(RoleNames.CasaConvivenciaDirector);

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(medicationId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_HoraInvalida_Devuelve400()
    {
        var (personId, medicationId) = await SeedResidentAsync();
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/medication-schemes", NewSchemeBody(medicationId, time: "25:00"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
