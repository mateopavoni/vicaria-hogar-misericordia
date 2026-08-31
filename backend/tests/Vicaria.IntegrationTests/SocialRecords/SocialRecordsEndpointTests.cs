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
}
