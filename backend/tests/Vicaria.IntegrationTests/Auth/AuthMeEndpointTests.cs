using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.IntegrationTests.Auth;

public class AuthMeEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly VicariaWebApplicationFactory _factory;

    public AuthMeEndpointTests(VicariaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    // el token ahora se valida contra un usuario real en la base (chequeo de sesion activa)
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

    [Fact]
    public async Task Me_SinToken_Retorna401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ConTokenInvalido_Retorna401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");

        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ConTokenValido_Retorna200ConLasClaims()
    {
        var actorId = await SembrarActorAsync(RoleNames.DirectoraDeCasona);
        var token = TestJwtFactory.CrearToken("Ana", "ana@mail.com", RoleNames.DirectoraDeCasona, actorId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ana", body);
        Assert.Contains(RoleNames.DirectoraDeCasona, body);
    }
}
