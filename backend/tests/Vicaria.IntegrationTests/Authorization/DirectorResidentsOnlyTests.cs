using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.Authorization;

// SCRUM-137: la Directora de Casa de Convivencia solo accede a Residentes en cualquier
// endpoint de la persona, no solo en el listado de fichas
public class DirectorResidentsOnlyTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DirectorResidentsOnlyTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task UsarTokenAsync(string rol)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var actor = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Actor",
            LastName = "Test",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "x",
            Status = UserStatus.Active,
            RoleId = db.Roles.First(r => r.Name == rol).Id,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(actor);
        await db.SaveChangesAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CreateToken("Test", actor.Email, rol, actor.Id));
    }

    private async Task<(Guid RecordId, Guid PersonId)> CrearFichaAsync(PersonType type)
    {
        await UsarTokenAsync(RoleNames.Referent);
        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Persona" });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var record = db.SocialRecords.First(r => r.Id == body!["id"]);
        record.PersonType = type;
        await db.SaveChangesAsync();
        return (body!["id"], body["personId"]);
    }

    [Theory]
    [InlineData("record")]
    [InlineData("observations")]
    [InlineData("timeline")]
    [InlineData("life-story")]
    [InlineData("stays")]
    public async Task Director_SobreAmbulatorio_Devuelve404(string recurso)
    {
        var (recordId, personId) = await CrearFichaAsync(PersonType.Ambulatory);
        await UsarTokenAsync(RoleNames.CasaConvivenciaDirector);

        var url = recurso switch
        {
            "record" => $"/api/social-records/{recordId}",
            "observations" => $"/api/persons/{personId}/observations",
            "timeline" => $"/api/persons/{personId}/timeline",
            "life-story" => $"/api/persons/{personId}/life-story",
            _ => $"/api/persons/{personId}/casa-convivencia-stays"
        };

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Director_SobreResidente_PuedeVerLaFicha()
    {
        var (recordId, _) = await CrearFichaAsync(PersonType.Resident);
        await UsarTokenAsync(RoleNames.CasaConvivenciaDirector);

        var response = await _client.GetAsync($"/api/social-records/{recordId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Director_RegistraAsistenciaDeAmbulatorio_Devuelve404()
    {
        var (_, personId) = await CrearFichaAsync(PersonType.Ambulatory);
        await UsarTokenAsync(RoleNames.CasaConvivenciaDirector);

        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Coordinador_SobreAmbulatorio_Devuelve404()
    {
        var (recordId, personId) = await CrearFichaAsync(PersonType.Ambulatory);
        await UsarTokenAsync(RoleNames.CasaConvivenciaCoordinator);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/social-records/{recordId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/persons/{personId}/observations")).StatusCode);
    }

    [Fact]
    public async Task Directora_ConteoPorFiltro_SoloCuentaResidentes()
    {
        await CrearFichaAsync(PersonType.Ambulatory);
        await UsarTokenAsync(RoleNames.CasaConvivenciaDirector);

        var withFilter = await _client.GetFromJsonAsync<Dictionary<string, int>>("/api/social-records/filter/count?PersonType=0");
        var residents = await _client.GetFromJsonAsync<Dictionary<string, int>>("/api/social-records/filter/count?PersonType=1");
        var noFilter = await _client.GetFromJsonAsync<Dictionary<string, int>>("/api/social-records/filter/count");

        // aunque pida ambulatorios, el conteo siempre se restringe a Residentes
        Assert.Equal(residents!["count"], withFilter!["count"]);
        Assert.Equal(residents["count"], noFilter!["count"]);
    }

    [Fact]
    public async Task Referente_SobreAmbulatorio_SigueTeniendoAcceso()
    {
        var (recordId, _) = await CrearFichaAsync(PersonType.Ambulatory);

        var response = await _client.GetAsync($"/api/social-records/{recordId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
