using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.ObservationCategories;

public class ObservationCategoryEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ObservationCategoryEndpointTests(VicariaWebApplicationFactory factory)
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

    private async Task<Guid> CrearCategoriaAsync(string rol = RoleNames.DirectoraDeCasona)
    {
        await UsarTokenAsync(rol);
        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "Convivencia", description = "Test" });
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        return body!["id"];
    }

    [Fact]
    public async Task Create_ComoDirectora_Devuelve201YCreaActiva()
    {
        await UsarTokenAsync(RoleNames.DirectoraDeCasona);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "Convivencia", description = "Notas de convivencia" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var category = await db.ObservationCategories.SingleAsync(c => c.Name == "Convivencia");
        Assert.Equal("Notas de convivencia", category.Description);
        Assert.True(category.IsActive);
        Assert.True(await db.AuditLogs.AnyAsync(a => a.AffectedEntity == $"ObservationCategory:{category.Id}"));
    }

    [Fact]
    public async Task Create_ComoCoordinador_Devuelve201()
    {
        await UsarTokenAsync(RoleNames.CoordinadorDeCasaConvivencia);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "Alimentacion" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ComoReferente_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Referente);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "X" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ComoEscucha_Devuelve403()
    {
        await UsarTokenAsync(RoleNames.Escucha);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "X" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_SinToken_Devuelve401()
    {
        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "X" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_NombreVacio_Devuelve400()
    {
        await UsarTokenAsync(RoleNames.DirectoraDeCasona);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "", description = "X" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ComoCoordinador_Devuelve204YActualiza()
    {
        var categoryId = await CrearCategoriaAsync(RoleNames.DirectoraDeCasona);
        await UsarTokenAsync(RoleNames.CoordinadorDeCasaConvivencia);

        var response = await _client.PutAsJsonAsync($"/api/observation-categories/{categoryId}", new { name = "Convivencia diaria", description = "Nueva desc" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var updated = await db.ObservationCategories.SingleAsync(c => c.Id == categoryId);
        Assert.Equal("Convivencia diaria", updated.Name);
        Assert.Equal("Nueva desc", updated.Description);
        Assert.True(await db.AuditLogs.AnyAsync(a => a.AffectedEntity == $"ObservationCategory:{categoryId}"));
    }

    [Fact]
    public async Task Update_CategoriaInexistente_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.DirectoraDeCasona);

        var response = await _client.PutAsJsonAsync($"/api/observation-categories/{Guid.NewGuid()}", new { name = "X" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_ComoDirectora_Devuelve204SinBorradoFisico()
    {
        var categoryId = await CrearCategoriaAsync();

        var response = await _client.PatchAsync($"/api/observation-categories/{categoryId}/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var deactivated = await db.ObservationCategories.SingleAsync(c => c.Id == categoryId);
        Assert.False(deactivated.IsActive);
        Assert.True(await db.AuditLogs.AnyAsync(a => a.AffectedEntity == $"ObservationCategory:{categoryId}"));
    }

    [Fact]
    public async Task Deactivate_CategoriaInexistente_Devuelve404()
    {
        await UsarTokenAsync(RoleNames.DirectoraDeCasona);

        var response = await _client.PatchAsync($"/api/observation-categories/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_YaDesactivada_Devuelve400()
    {
        var categoryId = await CrearCategoriaAsync();
        await _client.PatchAsync($"/api/observation-categories/{categoryId}/deactivate", null);

        var response = await _client.PatchAsync($"/api/observation-categories/{categoryId}/deactivate", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_ComoReferente_Devuelve403()
    {
        var categoryId = await CrearCategoriaAsync();

        await UsarTokenAsync(RoleNames.Referente);
        var response = await _client.PatchAsync($"/api/observation-categories/{categoryId}/deactivate", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}