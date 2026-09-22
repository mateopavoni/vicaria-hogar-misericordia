using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CrearToken("Test", "test@mail.com", rol, actorId));
    }

    [Fact]
    public async Task Create_ComoReferenteConSoloNombre_Devuelve201()
    {
        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ComoEscucha_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Escucha);

        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_SinNombre_Devuelve400()
    {
        await UsarTokenAsync(RoleNames.Referente);

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
        await UsarTokenAsync(RoleNames.Referente);
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ramón", lastName = "Gómez" });
        await UsarTokenAsync(RoleNames.Escucha);

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
        await UsarTokenAsync(RoleNames.Referente);
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
        await UsarTokenAsync(RoleNames.Referente);
        var nombreUnico = $"Nicolas{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/social-records", new { firstName = nombreUnico });

        await UsarTokenAsync(RoleNames.DirectoraDeCasona);
        var response = await _client.GetAsync($"/api/social-records/list?search={nombreUnico}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<SocialRecordListItemDto>>();
        Assert.Empty(resultado!.Items);
    }

    [Fact]
    public async Task Update_ComoReferente_Devuelve204()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var creada = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });
        var id = (await creada.Content.ReadFromJsonAsync<Dictionary<string, Guid>>())!["id"];

        var response = await _client.PutAsJsonAsync($"/api/social-records/{id}", new { firstName = "Ana", lastName = "Torres", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_ComoEscucha_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Escucha);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ComoCoordinador_Devuelve403()
    {
        // SCRUM-117: solo Referente y Directora pueden editar, a diferencia de crear
        await UsarTokenAsync(RoleNames.CoordinadorDeCasaConvivencia);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ConFichaInexistente_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/social-records/{Guid.NewGuid()}", new { firstName = "Ana", hasDocumentation = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_SinNombre_Devuelve400()
    {
        await UsarTokenAsync(RoleNames.Referente);

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
