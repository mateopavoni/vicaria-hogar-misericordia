using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Auth;
using Vicaria.Application.Common;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.IntegrationTests.Auth;

public class UserApprovalEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserApprovalEndpointTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static void UsarToken(HttpClient client, string rol, Guid? usuarioId = null) =>
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CrearToken("Test", "test@mail.com", rol, usuarioId));

    private async Task<Guid> RegistrarUsuarioPendienteAsync()
    {
        var dto = new RegisterDto("Ana", "Perez", $"{Guid.NewGuid()}@mail.com", "password123");
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["id"];
    }

    private Guid ObtenerRolIdSembrado()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        return db.Roles.First().Id;
    }

    [Fact]
    public async Task Pending_SinRolReferente_Retorna403()
    {
        UsarToken(_client, RoleNames.Escucha);

        var response = await _client.GetAsync("/api/auth/users/pending");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pending_ConRolReferente_IncluyeElUsuarioRecienRegistrado()
    {
        var usuarioId = await RegistrarUsuarioPendienteAsync();
        UsarToken(_client, RoleNames.Referente);

        var response = await _client.GetAsync("/api/auth/users/pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pendientes = await response.Content.ReadFromJsonAsync<PagedResult<PendingUserDto>>();
        Assert.Contains(pendientes!.Items, u => u.Id == usuarioId);
    }

    [Fact]
    public async Task Approve_UsuarioNoExiste_Retorna404()
    {
        UsarToken(_client, RoleNames.Referente);
        var rolId = ObtenerRolIdSembrado();

        var response = await _client.PostAsJsonAsync($"/api/auth/users/{Guid.NewGuid()}/approve", new ApproveUserDto(rolId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Approve_SinRolReferente_Retorna403()
    {
        var usuarioId = await RegistrarUsuarioPendienteAsync();
        UsarToken(_client, RoleNames.DirectoraDeCasona);
        var rolId = ObtenerRolIdSembrado();

        var response = await _client.PostAsJsonAsync($"/api/auth/users/{usuarioId}/approve", new ApproveUserDto(rolId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_ConRolReferenteYRolValido_Retorna204()
    {
        var usuarioId = await RegistrarUsuarioPendienteAsync();
        UsarToken(_client, RoleNames.Referente);
        var rolId = ObtenerRolIdSembrado();

        var response = await _client.PostAsJsonAsync($"/api/auth/users/{usuarioId}/approve", new ApproveUserDto(rolId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Approve_UsuarioYaAprobado_Retorna409()
    {
        var usuarioId = await RegistrarUsuarioPendienteAsync();
        UsarToken(_client, RoleNames.Referente);
        var rolId = ObtenerRolIdSembrado();
        await _client.PostAsJsonAsync($"/api/auth/users/{usuarioId}/approve", new ApproveUserDto(rolId));

        var response = await _client.PostAsJsonAsync($"/api/auth/users/{usuarioId}/approve", new ApproveUserDto(rolId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reject_ConMotivo_Retorna204()
    {
        var usuarioId = await RegistrarUsuarioPendienteAsync();
        UsarToken(_client, RoleNames.Referente);

        var response = await _client.PostAsJsonAsync($"/api/auth/users/{usuarioId}/reject", new RejectUserDto("no cumple los requisitos"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Reject_SinMotivo_Retorna400()
    {
        var usuarioId = await RegistrarUsuarioPendienteAsync();
        UsarToken(_client, RoleNames.Referente);

        var response = await _client.PostAsJsonAsync($"/api/auth/users/{usuarioId}/reject", new RejectUserDto(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
