using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.SocialRecords;

public class SocialRecordsEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SocialRecordsEndpointTests(VicariaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void UsarToken(string rol) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CrearToken("Test", "test@mail.com", rol, Guid.NewGuid()));

    [Fact]
    public async Task Create_ComoReferenteConSoloNombre_Devuelve201()
    {
        UsarToken(RoleNames.Referente);

        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ComoEscucha_Devuelve403()
    {
        UsarToken(RoleNames.Escucha);

        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_SinNombre_Devuelve400()
    {
        UsarToken(RoleNames.Referente);

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
        UsarToken(RoleNames.Referente);
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ramón", lastName = "Gómez" });
        UsarToken(RoleNames.Escucha);

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
    public async Task Update_ComoReferente_Devuelve204()
    {
        UsarToken(RoleNames.Referente);
        var creada = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });
        var id = (await creada.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        var response = await _client.PutAsJsonAsync($"/api/social-records/{id}", new { firstName = "Ana", lastName = "Torres", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_ComoEscucha_Devuelve403()
    {
        UsarToken(RoleNames.Escucha);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ComoCoordinador_Devuelve403()
    {
        // SCRUM-117: solo Referente y Directora pueden editar, a diferencia de crear
        UsarToken(RoleNames.CoordinadorDeCasaConvivencia);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ConFichaInexistente_Devuelve404()
    {
        UsarToken(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_SinNombre_Devuelve400()
    {
        UsarToken(RoleNames.Referente);

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
}
