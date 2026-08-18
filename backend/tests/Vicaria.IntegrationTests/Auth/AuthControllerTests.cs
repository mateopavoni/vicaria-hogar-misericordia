using System.Net;
using System.Net.Http.Json;
using Vicaria.Application.Auth;

namespace Vicaria.IntegrationTests.Auth;

public class AuthControllerTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(VicariaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ConDatosValidos_Retorna201()
    {
        var dto = new RegisterDto("Ana Perez", $"{Guid.NewGuid()}@mail.com", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ConEmailInvalido_Retorna400()
    {
        var dto = new RegisterDto("Ana Perez", "no-es-un-email", "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ConPasswordCorta_Retorna400()
    {
        var dto = new RegisterDto("Ana Perez", $"{Guid.NewGuid()}@mail.com", "123");

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ConEmailYaRegistrado_Retorna409()
    {
        var email = $"{Guid.NewGuid()}@mail.com";
        var dto = new RegisterDto("Ana Perez", email, "password123");
        await _client.PostAsJsonAsync("/api/auth/register", dto);

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
