using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Observations;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;
using Xunit;

namespace Vicaria.IntegrationTests.Observations;

public class ObservationCategoriesEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ObservationCategoriesEndpointTests(VicariaWebApplicationFactory factory)
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

    private async Task<Guid> UseTokenAsync(string role)
    {
        var actorId = await SeedActorAsync(role);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CrearToken("Test", "test@mail.com", role, actorId));
        return actorId;
    }

    private async Task<Guid> CreateCategoryAsync(string namePrefix)
    {
        await UseTokenAsync(RoleNames.Referente);
        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = $"{namePrefix}-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ObservationCategoryDto>();
        return body!.Id;
    }

    private async Task<ObservationCategory> SeedCategoryAsync(string name, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var category = new ObservationCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = null,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        db.ObservationCategories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    [Fact]
    public async Task Create_WithValidData_Returns201AndStoresDescription()
    {
        await UseTokenAsync(RoleNames.Referente);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "Vinculación", description = "Acompañamiento de vínculos" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ObservationCategoryDto>();
        Assert.NotNull(body);
        Assert.Equal("Acompañamiento de vínculos", body!.Description);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var category = await db.ObservationCategories.FindAsync(body.Id);
        Assert.Equal("Acompañamiento de vínculos", category!.Description);
        Assert.True(category.IsActive);
    }

    [Fact]
    public async Task Create_WithoutName_Returns400()
    {
        await UseTokenAsync(RoleNames.Referente);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDescriptionOver500Chars_Returns400()
    {
        await UseTokenAsync(RoleNames.Referente);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "Salud", description = new string('a', 501) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409()
    {
        await UseTokenAsync(RoleNames.Referente);
        await SeedCategoryAsync("Salud");

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "salud" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsEscucha_Returns403()
    {
        await UseTokenAsync(RoleNames.Escucha);

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name = "Salud" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/observation-categories", new { name = "Salud" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_RegistersAuditLogWithActor()
    {
        var actorId = await UseTokenAsync(RoleNames.Referente);
        var name = $"Salud-{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/observation-categories", new { name });

        var body = await response.Content.ReadFromJsonAsync<ObservationCategoryDto>();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var log = await db.AuditLogs.SingleAsync(a => a.AffectedEntity == $"ObservationCategory:{body!.Id}");
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Categoría de observación creada", log.Action);
    }

    [Fact]
    public async Task Update_WithValidData_Returns200AndChangesDescription()
    {
        var categoryId = await CreateCategoryAsync("Salud");
        var newName = $"Bienestar-{Guid.NewGuid():N}";

        var response = await _client.PutAsJsonAsync($"/api/observation-categories/{categoryId}", new { name = newName, description = "Nueva descripción" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ObservationCategoryDto>();
        Assert.Equal(newName, body!.Name);
        Assert.Equal("Nueva descripción", body.Description);
    }

    [Fact]
    public async Task Update_WithUnknownId_Returns404()
    {
        await UseTokenAsync(RoleNames.Referente);

        var response = await _client.PutAsJsonAsync($"/api/observation-categories/{Guid.NewGuid()}", new { name = "Salud" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDuplicateNameOfAnotherCategory_Returns409()
    {
        var categoryId = await CreateCategoryAsync("Salud");
        await SeedCategoryAsync("Legal");

        var response = await _client.PutAsJsonAsync($"/api/observation-categories/{categoryId}", new { name = "legal" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_RegistersAuditLogWithActor()
    {
        var categoryId = await CreateCategoryAsync("Salud");
        var actorId = await UseTokenAsync(RoleNames.Referente);

        await _client.PutAsJsonAsync($"/api/observation-categories/{categoryId}", new { name = $"Bienestar-{Guid.NewGuid():N}" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var auditLogs = db.AuditLogs.Where(a => a.AffectedEntity == $"ObservationCategory:{categoryId}").ToList();
        var log = auditLogs.Single(a => a.Action == "Categoría de observación actualizada");
        Assert.Equal(actorId, log.UserId);
    }

    [Fact]
    public async Task ToggleStatus_DeactivatesCategory_Returns204WithoutPhysicalDelete()
    {
        var categoryId = await CreateCategoryAsync("Salud");

        var response = await _client.PatchAsJsonAsync($"/api/observation-categories/{categoryId}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var category = await db.ObservationCategories.FindAsync(categoryId);
        Assert.NotNull(category);
        Assert.False(category!.IsActive);
    }

    [Fact]
    public async Task ToggleStatus_WithUnknownId_Returns404()
    {
        await UseTokenAsync(RoleNames.Referente);

        var response = await _client.PatchAsJsonAsync($"/api/observation-categories/{Guid.NewGuid()}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleStatus_AsDirectora_Returns403()
    {
        var categoryId = await CreateCategoryAsync("Salud");
        await UseTokenAsync(RoleNames.DirectoraDeCasona);

        var response = await _client.PatchAsJsonAsync($"/api/observation-categories/{categoryId}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ToggleStatus_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync($"/api/observation-categories/{Guid.NewGuid()}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ToggleStatus_RegistersAuditLogWithDeactivationAction()
    {
        var categoryId = await CreateCategoryAsync("Salud");
        var actorId = await UseTokenAsync(RoleNames.Referente);

        await _client.PatchAsJsonAsync($"/api/observation-categories/{categoryId}/status", new { isActive = false });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var log = await db.AuditLogs.SingleAsync(a => a.AffectedEntity == $"ObservationCategory:{categoryId}" && a.Action == "Categoría de observación desactivada");
        Assert.Equal(actorId, log.UserId);
    }

    [Fact]
    public async Task ToggleStatus_DeactivatedCategory_RejectsNewObservation_Returns400()
    {
        var categoryId = await CreateCategoryAsync("Salud");
        await _client.PatchAsJsonAsync($"/api/observation-categories/{categoryId}/status", new { isActive = false });

        var recordResponse = await _client.PostAsJsonAsync("/api/social-records", new { firstName = "Ana" });
        var personBody = await recordResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var personId = personBody!["personId"];

        var response = await _client.PostAsJsonAsync($"/api/persons/{personId}/observations", new { content = "Observación", categoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_OnlyActiveTrue_ReturnsOnlyActiveCategories()
    {
        await UseTokenAsync(RoleNames.Referente);
        var active = await SeedCategoryAsync("Activa");
        await SeedCategoryAsync("Inactiva", isActive: false);

        var response = await _client.GetAsync("/api/observation-categories?onlyActive=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<ObservationCategoryDto>>();
        Assert.NotNull(categories);
        Assert.Contains(active.Id, categories!.Select(c => c.Id));
        Assert.DoesNotContain(categories, c => c.Name == "Inactiva");
    }

    [Fact]
    public async Task Get_OnlyActiveFalse_ReturnsAllCategories()
    {
        await UseTokenAsync(RoleNames.Referente);
        await SeedCategoryAsync("Activa");
        await SeedCategoryAsync("Inactiva", isActive: false);

        var response = await _client.GetAsync("/api/observation-categories?onlyActive=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<ObservationCategoryDto>>();
        Assert.NotNull(categories);
        Assert.Contains(categories!, c => c.Name == "Activa");
        Assert.Contains(categories, c => c.Name == "Inactiva");
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/observation-categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsCoordinadorDeCasaConvivencia_Returns200()
    {
        await UseTokenAsync(RoleNames.CoordinadorDeCasaConvivencia);
        await SeedCategoryAsync("Salud");

        var response = await _client.GetAsync("/api/observation-categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}