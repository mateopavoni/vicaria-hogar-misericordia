using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.CasonaStays;

public class CasonaStayEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CasonaStayEndpointTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // firma el token con un usuario real seedeado en la base, no con un Guid random:
    // el OnTokenValidated de Program.cs valida el token contra un usuario existente y Active
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

    private async Task<Guid> UsarTokenAsync(string rol)
    {
        var actorId = await SembrarActorAsync(rol);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CrearToken("Test", "test@mail.com", rol, actorId));
        return actorId;
    }

    private async Task<Guid> CrearEstadiaActivaAsync()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var personId = body!["personId"];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var stay = new CasonaStay
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            EntryDate = DateTime.UtcNow
        };
        db.CasonaStays.Add(stay);
        await db.SaveChangesAsync();
        return stay.Id;
    }

    [Fact]
    public async Task Exit_ConEstadiaActivaYMotivo_Devuelve204()
    {
        var stayId = await CrearEstadiaActivaAsync();

        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 0 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Exit_SinMotivo_Devuelve204()
    {
        var stayId = await CrearEstadiaActivaAsync();

        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Exit_ConMotivoOtroYTexto_Devuelve204()
    {
        var stayId = await CrearEstadiaActivaAsync();

        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 4, reason = "Se retiró por motivos personales" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Exit_ConMotivoOtroSinTexto_Devuelve400()
    {
        var stayId = await CrearEstadiaActivaAsync();

        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 4 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Exit_ConEstadiaInexistente_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{Guid.NewGuid()}/egreso", new { exitReason = 0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Exit_ConEstadiaYaEgresada_Devuelve400()
    {
        var stayId = await CrearEstadiaActivaAsync();

        await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 0 });
        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Exit_ConEstadiaActiva_RegistraAuditLogConElActor()
    {
        var stayId = await CrearEstadiaActivaAsync();
        var actorId = await UsarTokenAsync(RoleNames.Referente);
        var beforeExit = DateTime.UtcNow;

        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 0 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var log = await db.AuditLogs.SingleAsync(a => a.AffectedEntity == $"CasonaStay:{stayId}");
        Assert.Equal(actorId, log.UserId);
        Assert.True(log.Date >= beforeExit);
    }

    [Fact]
    public async Task Exit_ComoEscucha_Devuelve403()
    {
        var stayId = await CrearEstadiaActivaAsync();
        await UsarTokenAsync(RoleNames.Escucha);

        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Exit_SinToken_Devuelve401()
    {
        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{Guid.NewGuid()}/egreso", new { exitReason = 0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Exit_ActualizaEstadoPersonAAmbulatorioYGeneraAuditorias_SCRUM147()
    {
        // Arrange
        await UsarTokenAsync(RoleNames.Referente);
        var responseCreate = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Carlos" });
        var body = await responseCreate.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var personId = body!["personId"];

        var stayId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            var sr = db.SocialRecords.First(s => s.PersonId == personId);
            sr.PersonType = PersonType.Resident;
            sr.Status = SocialRecordStatus.Active;

            db.CasonaStays.Add(new CasonaStay
            {
                Id = stayId,
                PersonId = personId,
                EntryDate = DateTime.UtcNow.AddDays(-10)
            });
            await db.SaveChangesAsync();
        }

        // Act: egreso especificando cambio a Ambulatorio Inactivo
        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new
        {
            exitReason = 0,
            newStatus = (int)SocialRecordStatus.Inactive
        });

        // Assert HTTP
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();

            var stay = db.CasonaStays.First(s => s.Id == stayId);
            Assert.NotNull(stay.ExitDate);

            var socialRecord = db.SocialRecords.First(s => s.PersonId == personId);
            Assert.Equal(PersonType.Ambulatory, socialRecord.PersonType);
            Assert.Equal(SocialRecordStatus.Inactive, socialRecord.Status);

            var auditLogs = db.AuditLogs
                .Where(a => a.AffectedEntity == $"CasonaStay:{stayId}" || a.AffectedEntity == $"Person:{personId}")
                .ToList();
            Assert.Equal(2, auditLogs.Count);
        }
    }

    [Fact]
    public async Task Exit_SinEspecificarNewStatus_PasaAAmbulatorioActivoPorDefecto_SCRUM147()
    {
        await UsarTokenAsync(RoleNames.Referente);
        var responseCreate = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Martin" });
        var body = await responseCreate.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var personId = body!["personId"];

        var stayId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            var sr = db.SocialRecords.First(s => s.PersonId == personId);
            sr.PersonType = PersonType.Resident;

            db.CasonaStays.Add(new CasonaStay
            {
                Id = stayId,
                PersonId = personId,
                EntryDate = DateTime.UtcNow.AddDays(-5)
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 1 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();

            var socialRecord = db.SocialRecords.First(s => s.PersonId == personId);
            Assert.Equal(PersonType.Ambulatory, socialRecord.PersonType);
            Assert.Equal(SocialRecordStatus.Active, socialRecord.Status);
        }
    }
}