using System.Net;
using System.Net.Http.Headers;
using Vicaria.Domain.Entities;

namespace Vicaria.IntegrationTests.Auth;

public class AuthMeEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthMeEndpointTests(VicariaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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
        var token = TestJwtFactory.CrearToken("Ana", "ana@mail.com", RoleNames.DirectoraDeCasona);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ana", body);
        Assert.Contains(RoleNames.DirectoraDeCasona, body);
    }
}
