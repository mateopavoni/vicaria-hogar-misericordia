using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Timelines;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;
using Xunit;

namespace Vicaria.IntegrationTests.Timelines;

public class ProfileTimelineEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProfileTimelineEndpointTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task UsarTokenAsync(string rol)
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

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CrearToken("Test", "test@mail.com", rol, actor.Id));
    }

    private async Task<Guid> CrearPersonaAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "PersonaTest" });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["personId"];
    }

    [Fact]
    public async Task GetTimeline_ConObservacionesYEstadias_DevuelveEntradasCombinadasYOrdenadas()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var personId = await CrearPersonaAsync();

        Guid authorId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            authorId = db.Users.First(u => u.FirstName == "Actor").Id;

            var category = new ObservationCategory { Id = Guid.NewGuid(), Name = "Salud", IsActive = true };
            db.ObservationCategories.Add(category);

            db.Observations.Add(new Observation
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Content = "Obs reciente",
                CategoryId = category.Id,
                AuthorUserId = authorId,
                CreatedAt = DateTime.UtcNow
            });

            db.CasaConvivenciaStays.Add(new CasaConvivenciaStay
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                EntryDate = DateTime.UtcNow.AddDays(-10),
                ExitDate = DateTime.UtcNow.AddDays(-5),
                ExitReason = StayExitReason.VoluntaryDischarge
            });

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/persons/{personId}/timeline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProfileTimelineResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(3, result!.TotalCount);
        Assert.Equal(3, result.Items.Count);

        for (var i = 1; i < result.Items.Count; i++)
        {
            Assert.True(result.Items[i - 1].Date >= result.Items[i].Date);
        }

        Assert.Contains(result.Items, e => e.Type == ProfileTimelineEntryType.Observation && e.Content == "Obs reciente" && e.CategoryName == "Salud" && e.AuthorName != null);
        Assert.Contains(result.Items, e => e.Type == ProfileTimelineEntryType.CasaConvivenciaStayEntry && e.Title == "Ingreso a la Casa de Convivencia");
        Assert.Contains(result.Items, e => e.Type == ProfileTimelineEntryType.CasaConvivenciaStayExit && e.Title == "Egreso de la Casa de Convivencia");
    }

    [Fact]
    public async Task GetTimeline_PersonaInexistente_DevuelveNotFound()
    {
        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.GetAsync($"/api/persons/{Guid.NewGuid()}/timeline");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTimeline_EscuchaPuedeLeerTimeline()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var personId = await CrearPersonaAsync();
        await UsarTokenAsync(RoleNames.Escucha);

        var response = await _client.GetAsync($"/api/persons/{personId}/timeline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProfileTimelineResponseDto>();
        Assert.NotNull(result);
        Assert.Empty(result!.Items);
    }
}