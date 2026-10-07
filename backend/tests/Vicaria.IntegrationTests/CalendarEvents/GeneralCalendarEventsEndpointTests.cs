using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;
using Xunit;

namespace Vicaria.IntegrationTests.CalendarEvents;

public class GeneralCalendarEventsEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GeneralCalendarEventsEndpointTests(VicariaWebApplicationFactory factory)
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

    private static object ValidPayload() => new
    {
        title = "Merienda",
        date = "2026-10-10T00:00:00Z",
        startTime = "16:00:00",
        endTime = "18:00:00",
        description = "Actividad en el patio",
        recurrenceDays = (int)WeekDays.Tuesday
    };

    [Fact]
    public async Task Post_WithReferentRole_Returns201WithId()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync("/api/general-calendar-events", ValidPayload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body["id"]);
    }

    [Theory]
    [InlineData(RoleNames.Listener)]
    [InlineData(RoleNames.CasaConvivenciaDirector)]
    [InlineData(RoleNames.CasaConvivenciaCoordinator)]
    public async Task Post_WithNonReferentRoles_Returns403(string role)
    {
        await UseTokenAsync(role);

        var response = await _client.PostAsJsonAsync("/api/general-calendar-events", ValidPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/general-calendar-events", ValidPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithMissingTitle_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var payload = new { title = "", date = "2026-10-10T00:00:00Z" };

        var response = await _client.PostAsJsonAsync("/api/general-calendar-events", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithEndTimeNotAfterStartTime_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var payload = new
        {
            title = "Merienda",
            date = "2026-10-10T00:00:00Z",
            startTime = "18:00:00",
            endTime = "16:00:00"
        };

        var response = await _client.PostAsJsonAsync("/api/general-calendar-events", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithRange_ExpandsSeedTemplatesAndPaginates()
    {
        await UseTokenAsync(RoleNames.Referent);

        // semana completa L-V: Desayuno y Almuerzo expanden 5 días, Merendero solo el martes
        var response = await _client.GetAsync("/api/general-calendar-events?from=2026-10-05&to=2026-10-09&page=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(11, body.GetProperty("total").GetInt32());
        Assert.Equal(2, body.GetProperty("totalPages").GetInt32());
        Assert.Equal(10, body.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData(RoleNames.Referent)]
    [InlineData(RoleNames.Listener)]
    [InlineData(RoleNames.CasaConvivenciaDirector)]
    [InlineData(RoleNames.CasaConvivenciaCoordinator)]
    public async Task Get_WithEachAuthorizedRole_Returns200(string role)
    {
        await UseTokenAsync(role);

        var response = await _client.GetAsync("/api/general-calendar-events?from=2026-10-05&to=2026-10-09");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutRange_Returns200WithPagedShape()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync("/api/general-calendar-events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("items", out _));
        Assert.True(body.TryGetProperty("total", out _));
        Assert.True(body.TryGetProperty("totalPages", out _));
    }

    [Fact]
    public async Task Get_WithFromAfterTo_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync("/api/general-calendar-events?from=2026-10-11&to=2026-10-05");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("from", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/general-calendar-events?from=2026-10-05&to=2026-10-09");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_KnownSeedTemplate_Returns200()
    {
        await UseTokenAsync(RoleNames.Referent);

        // id fijo del seed de SCRUM-183 (Desayuno)
        var response = await _client.GetAsync("/api/general-calendar-events/88888888-8888-8888-8888-888888888801");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Desayuno", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync($"/api/general-calendar-events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
