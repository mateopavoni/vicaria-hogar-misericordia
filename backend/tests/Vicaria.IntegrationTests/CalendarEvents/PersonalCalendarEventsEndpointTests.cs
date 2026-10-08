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

public class PersonalCalendarEventsEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PersonalCalendarEventsEndpointTests(VicariaWebApplicationFactory factory)
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

    private async Task<Guid> SeedUserWithoutRoleAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Other",
            LastName = "Test",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "x",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> UseTokenAsync(string role)
    {
        var actorId = await SeedActorAsync(role);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CreateToken("Test", "test@mail.com", role, actorId));
        return actorId;
    }

    private async Task<Guid> SeedPersonalEventAsync(Guid authorId, string title, DateTime date)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();

        var calendarEvent = new PersonalCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = title,
            Date = date,
            RecurrenceDays = WeekDays.None,
            AuthorUserId = authorId,
            CreatedAt = DateTime.UtcNow
        };
        db.PersonalCalendarEvents.Add(calendarEvent);
        await db.SaveChangesAsync();
        return calendarEvent.Id;
    }

    [Fact]
    public async Task Get_ReturnsOnlyEventsOwnedByCaller()
    {
        var actorId = await UseTokenAsync(RoleNames.Referent);
        var otherId = await SeedUserWithoutRoleAsync();
        var ownEventId = await SeedPersonalEventAsync(actorId, "De A", new DateTime(2026, 10, 6));
        await SeedPersonalEventAsync(otherId, "De B", new DateTime(2026, 10, 6));

        var response = await _client.GetAsync("/api/personal-calendar-events?from=2026-10-05&to=2026-10-11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        var item = body.GetProperty("items")[0];
        Assert.Equal("De A", item.GetProperty("title").GetString());
        Assert.Equal(ownEventId, item.GetProperty("eventId").GetGuid());
    }

    [Theory]
    [InlineData(RoleNames.Referent)]
    [InlineData(RoleNames.Listener)]
    [InlineData(RoleNames.CasaConvivenciaDirector)]
    [InlineData(RoleNames.CasaConvivenciaCoordinator)]
    public async Task Get_WithEachAuthorizedRole_Returns200(string role)
    {
        await UseTokenAsync(role);

        var response = await _client.GetAsync("/api/personal-calendar-events?from=2026-10-05&to=2026-10-11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithFromAfterTo_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync("/api/personal-calendar-events?from=2026-10-11&to=2026-10-05");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("from", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/personal-calendar-events?from=2026-10-05&to=2026-10-11");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_OwnEvent_Returns200()
    {
        var actorId = await UseTokenAsync(RoleNames.Referent);
        var ownEventId = await SeedPersonalEventAsync(actorId, "Mi evento", new DateTime(2026, 12, 1));

        var response = await _client.GetAsync($"/api/personal-calendar-events/{ownEventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Mi evento", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetById_OtherUsersEvent_Returns404()
    {
        var actorId = await UseTokenAsync(RoleNames.Referent);
        var otherId = await SeedUserWithoutRoleAsync();
        var foreignEventId = await SeedPersonalEventAsync(otherId, "De otro", new DateTime(2026, 12, 1));

        var response = await _client.GetAsync($"/api/personal-calendar-events/{foreignEventId}");

        // mismo 404 que un id inexistente: no revelar que el evento existe
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("no existe", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync($"/api/personal-calendar-events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("no existe", await response.Content.ReadAsStringAsync());
    }
}
