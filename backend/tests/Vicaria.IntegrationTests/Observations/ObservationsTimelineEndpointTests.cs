using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Observations;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;
using Xunit;

namespace Vicaria.IntegrationTests.Observations;

public class ObservationsTimelineEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ObservationsTimelineEndpointTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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
        await UsarTokenAsync(RoleNames.Referente);
        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "PersonaTest" });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["personId"];
    }

    [Fact]
    public async Task GetTimeline_FiltradoPorCategoria_DevuelveConteoCorrectoYElementosCoincidentes()
    {
        var personId = await CrearPersonaAsync();
        var catSaludId = Guid.NewGuid();
        var catLegalId = Guid.NewGuid();
        Guid authorId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            authorId = db.Users.First().Id;

            db.ObservationCategories.AddRange(
                new ObservationCategory { Id = catSaludId, Name = "Salud", IsActive = true },
                new ObservationCategory { Id = catLegalId, Name = "Legal", IsActive = true }
            );

            db.Observations.AddRange(
                new Observation { Id = Guid.NewGuid(), PersonId = personId, CategoryId = catSaludId, AuthorUserId = authorId, Content = "Obs 1", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new Observation { Id = Guid.NewGuid(), PersonId = personId, CategoryId = catSaludId, AuthorUserId = authorId, Content = "Obs 2", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new Observation { Id = Guid.NewGuid(), PersonId = personId, CategoryId = catLegalId, AuthorUserId = authorId, Content = "Obs 3", CreatedAt = DateTime.UtcNow }
            );

            await db.SaveChangesAsync();
        }

        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.GetAsync($"/api/persons/{personId}/observations?categoryId={catSaludId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ObservationsTimelineResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal(catSaludId, item.CategoryId));
    }

    [Fact]
    public async Task GetTimeline_SinFiltros_DevuelveTotalCompletoYOrdenDescendente()
    {
        // Arrange
        var personId = await CrearPersonaAsync();
        Guid authorId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            authorId = db.Users.First().Id;

            db.Observations.AddRange(
                new Observation { Id = Guid.NewGuid(), PersonId = personId, AuthorUserId = authorId, Content = "Antigua", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new Observation { Id = Guid.NewGuid(), PersonId = personId, AuthorUserId = authorId, Content = "Reciente", CreatedAt = DateTime.UtcNow }
            );

            await db.SaveChangesAsync();
        }

        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.GetAsync($"/api/persons/{personId}/observations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ObservationsTimelineResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalCount);
        Assert.True(result.Items[0].CreatedAt > result.Items[1].CreatedAt);
    }

    [Fact]
    public async Task GetTimeline_AsCoordinadorDeCasaConvivencia_Returns200()
    {
        var personId = await CrearPersonaAsync();
        await UsarTokenAsync(RoleNames.CoordinadorDeCasaConvivencia);

        var response = await _client.GetAsync($"/api/persons/{personId}/observations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ObservationsTimelineResponseDto>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
    }

    [Fact]
    public async Task CreateObservation_AsCoordinadorDeCasaConvivencia_Returns201()
    {
        var personId = await CrearPersonaAsync();
        await UsarTokenAsync(RoleNames.CoordinadorDeCasaConvivencia);

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/observations", new { content = "Observación del coordinador" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Export_DevuelveCsvConLasObservacionesFiltradas()
    {
        var personId = await CrearPersonaAsync();
        var catSaludId = Guid.NewGuid();
        Guid authorId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            authorId = db.Users.First().Id;

            db.ObservationCategories.Add(new ObservationCategory { Id = catSaludId, Name = "Salud", IsActive = true });
            db.Observations.AddRange(
                new Observation { Id = Guid.NewGuid(), PersonId = personId, CategoryId = catSaludId, AuthorUserId = authorId, Content = "Con categoría", CreatedAt = DateTime.UtcNow },
                new Observation { Id = Guid.NewGuid(), PersonId = personId, AuthorUserId = authorId, Content = "Sin categoría", CreatedAt = DateTime.UtcNow.AddDays(-1) }
            );
            await db.SaveChangesAsync();
        }

        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.GetAsync($"/api/persons/{personId}/observations/export?categoryId={catSaludId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Fecha,Categoría,Autor,Contenido", csv);
        Assert.Contains("Con categoría", csv);
        Assert.DoesNotContain("Sin categoría", csv);
    }

    [Fact]
    public async Task Export_SinToken_Devuelve401()
    {
        var response = await _client.GetAsync($"/api/persons/{Guid.NewGuid()}/observations/export");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}