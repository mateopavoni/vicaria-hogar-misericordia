using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    private void UsarToken(string rol) =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CrearToken("Test", "test@mail.com", rol, Guid.NewGuid()));

    private async Task<Guid> CrearEstadiaActivaAsync()
    {
        UsarToken(RoleNames.Referente);
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
        UsarToken(RoleNames.Referente);
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
    public async Task Exit_ComoEscucha_Devuelve403()
    {
        var stayId = await CrearEstadiaActivaAsync();
        UsarToken(RoleNames.Escucha);

        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{stayId}/egreso", new { exitReason = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Exit_SinToken_Devuelve401()
    {
        var response = await _client.PutAsJsonAsync($"/api/casona-stays/{Guid.NewGuid()}/egreso", new { exitReason = 0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}