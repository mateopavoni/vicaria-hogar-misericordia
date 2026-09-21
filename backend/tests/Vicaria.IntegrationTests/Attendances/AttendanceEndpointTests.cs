using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.Attendances;

public class AttendanceEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AttendanceEndpointTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // el token se valida contra un usuario real en la base (chequeo de sesión activa)
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
        var response = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["personId"];
    }

    private async Task RegistrarAsistenciaAsync(Guid personId)
    {
        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Register_ComoEscucha_Devuelve204()
    {
        var personId = await CrearPersonaAsync();
        await UsarTokenAsync(RoleNames.Escucha);

        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Register_ConFichaActiva_RegistraAsistenciaYNoCambiaEstado()
    {
        var personId = await CrearPersonaAsync();

        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var record = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Active, record.Status);
        Assert.True(await db.Attendances.AnyAsync(a => a.PersonId == personId));
    }

    [Fact]
    public async Task Register_ConFichaInactiva_ReactivaYRegistraAuditLog()
    {
        var personId = await CrearPersonaAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            var record = await db.SocialRecords.SingleAsync(r => r.PersonId == personId);
            record.Status = SocialRecordStatus.Inactive;
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var reactivada = await verifyDb.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Active, reactivada.Status);
    }

    [Fact]
    public async Task Register_PersonaInexistente_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Register_PersonaSinFicha_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.Referente);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "SinFicha", CreatedAt = DateTime.UtcNow };
        db.People.Add(person);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId = person.Id });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Register_PersonIdVacio_Devuelve400()
    {
        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId = Guid.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_SinToken_Devuelve401()
    {
        var response = await _client.PostAsJsonAsync("/api/attendance", new { personId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckInactivity_SinAsistenciaReciente_PasaAInactivo()
    {
        // ficha con asistencia vieja (más de 30 días) y otra con asistencia reciente
        var personId = await CrearPersonaAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            db.Attendances.Add(new Attendance
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Date = DateTime.UtcNow.AddDays(-31),
                CreatedByUserId = Guid.NewGuid()
            });
            await db.SaveChangesAsync();
        }

        var recentPersonId = await CrearPersonaAsync();
        await RegistrarAsistenciaAsync(recentPersonId);

        using var processScope = _factory.Services.CreateScope();
        var service = processScope.ServiceProvider.GetRequiredService<IAttendanceInactivityService>();
        await service.CheckAndProcessInactivityAsync();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var inactiva = await verifyDb.SocialRecords.SingleAsync(r => r.PersonId == personId);
        Assert.Equal(SocialRecordStatus.Inactive, inactiva.Status);
        var activa = await verifyDb.SocialRecords.SingleAsync(r => r.PersonId == recentPersonId);
        Assert.Equal(SocialRecordStatus.Active, activa.Status);
    }
}