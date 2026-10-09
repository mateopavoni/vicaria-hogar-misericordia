using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;
using Xunit;

namespace Vicaria.IntegrationTests.CasonaVisits;

public class CasonaVisitsEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CasonaVisitsEndpointTests(VicariaWebApplicationFactory factory)
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

    // persona con ficha del tipo indicado y estadía opcional, más el usuario dueño
    // de la ficha (FK CreatedByUserId). Cada test llama a esto con su propio residente.
    private async Task<Guid> SeedResidentAsync(
        PersonType personType = PersonType.Resident,
        bool openStay = true)
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
        db.CasaConvivenciaStays.Add(new CasaConvivenciaStay
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            EntryDate = DateTime.UtcNow.AddDays(-10),
            ExitDate = openStay ? null : DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        return person.Id;
    }

    private async Task SeedVisitAsync(
        Guid personId,
        DateTime date,
        TimeSpan startTime,
        int durationMinutes = 60,
        VisitStatus status = VisitStatus.Pending)
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
        db.Users.Add(owner);
        db.CasonaVisits.Add(new CasonaVisit
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            VisitorName = "Visitante Seed",
            Date = date.Date,
            StartTime = startTime,
            EstimatedDurationMinutes = durationMinutes,
            Status = status,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static string IsoDate(int daysFromToday) =>
        DateTime.UtcNow.Date.AddDays(daysFromToday).ToString("yyyy-MM-dd'T'00:00:00Z");

    private static object ValidPayload(Guid personId, string date, string startTime = "16:00:00") => new
    {
        personId,
        visitorName = "María Visitante",
        date,
        startTime,
        estimatedDurationMinutes = 60
    };

    private static object UpdatePayload(
        Guid personId,
        string date,
        string startTime = "16:00:00",
        int status = 0,
        string? cancellationReason = null) => new
    {
        personId,
        visitorName = "María Visitante",
        date,
        startTime,
        estimatedDurationMinutes = 60,
        status,
        cancellationReason
    };

    [Theory]
    [InlineData(RoleNames.Referent, 9)]
    [InlineData(RoleNames.CasaConvivenciaDirector, 10)]
    [InlineData(RoleNames.CasaConvivenciaCoordinator, 11)]
    public async Task Post_WithEachAuthorizedRole_Returns201WithId(string role, int hour)
    {
        await UseTokenAsync(role);
        var personId = await SeedResidentAsync();

        var payload = ValidPayload(personId, IsoDate(15), $"{hour:00}:00:00");
        var response = await _client.PostAsJsonAsync("/api/casona-visits", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body["id"]);
    }

    [Fact]
    public async Task Post_WithListenerRole_Returns403()
    {
        await UseTokenAsync(RoleNames.Listener);
        var personId = await SeedResidentAsync();

        var response = await _client.PostAsJsonAsync("/api/casona-visits", ValidPayload(personId, IsoDate(15)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var personId = await SeedResidentAsync();

        var response = await _client.PostAsJsonAsync("/api/casona-visits", ValidPayload(personId, IsoDate(15)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithMissingFields_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync("/api/casona-visits", new
        {
            personId = Guid.Empty,
            visitorName = "",
            date = IsoDate(15),
            startTime = "16:00:00",
            estimatedDurationMinutes = 60
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithPastDate_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/casona-visits", ValidPayload(personId, IsoDate(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithZeroDuration_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();

        var payload = new
        {
            personId,
            visitorName = "María Visitante",
            date = IsoDate(16),
            startTime = "16:00:00",
            estimatedDurationMinutes = 0
        };
        var response = await _client.PostAsJsonAsync("/api/casona-visits", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithUnknownPerson_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync(
            "/api/casona-visits", ValidPayload(Guid.NewGuid(), IsoDate(16)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("no existe", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_WithAmbulatoryPerson_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync(personType: PersonType.Ambulatory);

        var response = await _client.PostAsJsonAsync("/api/casona-visits", ValidPayload(personId, IsoDate(16)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Residente", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_WithoutOpenStay_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync(openStay: false);

        var response = await _client.PostAsJsonAsync("/api/casona-visits", ValidPayload(personId, IsoDate(16)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("estadía abierta", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_OverlappingVisit_Returns409()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(14), new TimeSpan(16, 0, 0));

        var response = await _client.PostAsJsonAsync(
            "/api/casona-visits", ValidPayload(personId, IsoDate(14), "16:30:00"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_OnPendingVisit_Returns204()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(17), new TimeSpan(10, 0, 0));
        var visitId = GetVisitId(personId, DateTime.UtcNow.Date.AddDays(17));

        var response = await _client.PutAsJsonAsync(
            $"/api/casona-visits/{visitId}",
            UpdatePayload(personId, IsoDate(17), "10:00:00"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_OnCompletedVisit_Returns409()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(
            personId, DateTime.UtcNow.Date.AddDays(18), new TimeSpan(11, 0, 0), status: VisitStatus.Completed);
        var visitId = GetVisitId(personId, DateTime.UtcNow.Date.AddDays(18));

        var response = await _client.PutAsJsonAsync(
            $"/api/casona-visits/{visitId}",
            UpdatePayload(personId, IsoDate(18), "11:00:00"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_CancelledWithoutReason_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(19), new TimeSpan(12, 0, 0));
        var visitId = GetVisitId(personId, DateTime.UtcNow.Date.AddDays(19));

        var response = await _client.PutAsJsonAsync(
            $"/api/casona-visits/{visitId}",
            UpdatePayload(personId, IsoDate(19), "12:00:00", status: (int)VisitStatus.Cancelled));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("motivo", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Put_WithInvalidStatusValue_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(21), new TimeSpan(13, 0, 0));
        var visitId = GetVisitId(personId, DateTime.UtcNow.Date.AddDays(21));

        var response = await _client.PutAsJsonAsync(
            $"/api/casona-visits/{visitId}",
            UpdatePayload(personId, IsoDate(21), "13:00:00", status: 99));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_MovingVisitToPast_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(22), new TimeSpan(14, 0, 0));
        var visitId = GetVisitId(personId, DateTime.UtcNow.Date.AddDays(22));

        var response = await _client.PutAsJsonAsync(
            $"/api/casona-visits/{visitId}",
            UpdatePayload(personId, IsoDate(-2), "14:00:00"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("pasado", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Put_UnknownVisit_Returns404()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/casona-visits/{Guid.NewGuid()}",
            UpdatePayload(personId, IsoDate(20)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithListenerRole_Returns403()
    {
        await UseTokenAsync(RoleNames.Listener);
        var personId = await SeedResidentAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/casona-visits/{Guid.NewGuid()}",
            UpdatePayload(personId, IsoDate(20)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(RoleNames.Referent)]
    [InlineData(RoleNames.CasaConvivenciaDirector)]
    [InlineData(RoleNames.CasaConvivenciaCoordinator)]
    public async Task Get_WithEachAuthorizedRole_Returns200(string role)
    {
        await UseTokenAsync(role);

        var response = await _client.GetAsync("/api/casona-visits?from=2026-10-01&to=2026-12-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsOnlyVisitsInRangeWithPagedShape()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(25), new TimeSpan(9, 0, 0));
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(25), new TimeSpan(15, 0, 0));
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(26), new TimeSpan(10, 0, 0));

        var response = await _client.GetAsync(
            $"/api/casona-visits?from={IsoDate(25)}&to={IsoDate(25)}&page=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("total").GetInt32());
        Assert.Equal(1, body.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());

        var first = body.GetProperty("items")[0];
        Assert.Equal("Carla Residente", first.GetProperty("personName").GetString());
        Assert.Equal("Visitante Seed", first.GetProperty("visitorName").GetString());
        Assert.Equal((int)VisitStatus.Pending, first.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Get_WithoutRange_Returns200WithPagedShape()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync("/api/casona-visits");

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

        var response = await _client.GetAsync("/api/casona-visits?from=2026-10-11&to=2026-10-05");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("from", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/casona-visits?from=2026-10-01&to=2026-12-31");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // cambio de estado dedicado (SCRUM-211): PATCH api/casona-visits/{id}/status
    [Fact]
    public async Task Patch_ToCompleted_Returns204AndPersistsStatus()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(30), new TimeSpan(10, 0, 0));
        var visitId = GetVisitId(personId, DateTime.UtcNow.Date.AddDays(30));

        var response = await _client.PatchAsJsonAsync($"/api/casona-visits/{visitId}/status", new { status = 1 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        Assert.Equal(VisitStatus.Completed, db.CasonaVisits.First(v => v.Id == visitId).Status);
    }

    [Fact]
    public async Task Patch_ToCancelled_StoresReason()
    {
        await UseTokenAsync(RoleNames.CasaConvivenciaDirector);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(31), new TimeSpan(10, 0, 0));
        var visitId = GetVisitId(personId, DateTime.UtcNow.Date.AddDays(31));

        var response = await _client.PatchAsJsonAsync(
            $"/api/casona-visits/{visitId}/status", new { status = 2, cancellationReason = "El visitante avisó que no viene" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var visit = db.CasonaVisits.First(v => v.Id == visitId);
        Assert.Equal(VisitStatus.Cancelled, visit.Status);
        Assert.Equal("El visitante avisó que no viene", visit.CancellationReason);
    }

    [Fact]
    public async Task Patch_UnknownVisit_Returns404()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PatchAsJsonAsync($"/api/casona-visits/{Guid.NewGuid()}/status", new { status = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_WithInvalidStatusValue_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);
        var personId = await SeedResidentAsync();
        await SeedVisitAsync(personId, DateTime.UtcNow.Date.AddDays(32), new TimeSpan(10, 0, 0));
        var visitId = GetVisitId(personId, DateTime.UtcNow.Date.AddDays(32));

        var response = await _client.PatchAsJsonAsync($"/api/casona-visits/{visitId}/status", new { status = 9 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_WithListenerRole_Returns403()
    {
        await UseTokenAsync(RoleNames.Listener);

        var response = await _client.PatchAsJsonAsync($"/api/casona-visits/{Guid.NewGuid()}/status", new { status = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // SCRUM-212: la escucha tampoco puede consultar visitas
    [Fact]
    public async Task Get_WithListenerRole_Returns403()
    {
        await UseTokenAsync(RoleNames.Listener);

        var response = await _client.GetAsync("/api/casona-visits?from=2026-10-01&to=2026-12-31");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private Guid GetVisitId(Guid personId, DateTime date)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var visit = db.CasonaVisits.First(v => v.PersonId == personId && v.Date == date.Date);
        return visit.Id;
    }
}
