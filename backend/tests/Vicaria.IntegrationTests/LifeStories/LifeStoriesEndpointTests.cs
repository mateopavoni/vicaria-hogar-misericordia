using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.LifeStories;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.LifeStories;

public class LifeStoriesEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly VicariaWebApplicationFactory _factory;

    public LifeStoriesEndpointTests(VicariaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

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

    private async Task<Guid> CrearPersonaAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });
        var creada = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return creada!["personId"];
    }

    [Fact]
    public async Task PutStage_ComoReferente_Devuelve200YEditaEtapa()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var personId = await CrearPersonaAsync();

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/life-story/in-hogar", new { content = "En el hogar" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<LifeStoryResponseDto>();
        Assert.NotNull(resultado);
        Assert.Equal("En el hogar", resultado!.InHogar.Content);
        Assert.True(resultado.InHogar.IsCompleted);
    }

    [Fact]
    public async Task PutStage_EtapaInvalida_Devuelve400()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var personId = await CrearPersonaAsync();

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/life-story/etapa-inventada", new { content = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutStage_ContentVacio_Devuelve400()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var personId = await CrearPersonaAsync();

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/life-story/before-hogar", new { content = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutStage_PersonaInexistente_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/persons/{Guid.NewGuid()}/life-story/before-hogar", new { content = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutStage_ComoEscucha_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var personId = await CrearPersonaAsync();
        await UsarTokenAsync(RoleNames.Escucha);

        var response = await _client.PutAsJsonAsync($"/api/persons/{personId}/life-story/before-hogar", new { content = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutStage_SinToken_Devuelve401()
    {
        var response = await _client.PutAsJsonAsync($"/api/persons/{Guid.NewGuid()}/life-story/before-hogar", new { content = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ComoReferente_Devuelve200()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var personId = await CrearPersonaAsync();

        var response = await _client.GetAsync($"/api/persons/{personId}/life-story");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultado = await response.Content.ReadFromJsonAsync<LifeStoryResponseDto>();
        Assert.NotNull(resultado);
        Assert.Equal(personId, resultado!.PersonId);
    }
}