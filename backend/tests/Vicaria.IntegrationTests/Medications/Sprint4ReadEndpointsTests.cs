using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.Medications;

// listados de lectura que usa el frontend del Sprint 4: stock completo, catálogo de
// medicamentos, asistencia de un día y PDF del resumen mensual
public class Sprint4ReadEndpointsTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Sprint4ReadEndpointsTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task UseTokenAsync(string role)
    {
        Guid actorId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Actor",
                LastName = "Test",
                Email = $"{Guid.NewGuid()}@mail.com",
                PasswordHash = "x",
                Status = UserStatus.Active,
                RoleId = db.Roles.First(r => r.Name == role).Id,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            actorId = user.Id;
        }

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CreateToken("Test", "test@mail.com", role, actorId));
    }

    private async Task<string> SeedMedicationAsync(int stock)
    {
        var name = $"Med {Guid.NewGuid():N}";
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        db.MedicationCatalogs.Add(new MedicationCatalog { Id = Guid.NewGuid(), Name = name, Stock = stock, MinimumStockThreshold = 5 });
        await db.SaveChangesAsync();
        return name;
    }

    [Fact]
    public async Task GetStockOverview_AsReferent_ReturnsFullInventoryWithThreshold()
    {
        var name = await SeedMedicationAsync(stock: 50);
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync("/api/stock-medicamentos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = items.EnumerateArray().First(i => i.GetProperty("medicationName").GetString() == name);
        Assert.Equal(50, item.GetProperty("currentStock").GetInt32());
        Assert.Equal(5, item.GetProperty("minimumStockThreshold").GetInt32());
    }

    [Fact]
    public async Task GetStockOverview_AsListener_Returns403()
    {
        await UseTokenAsync(RoleNames.Listener);

        var response = await _client.GetAsync("/api/stock-medicamentos");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCatalog_AsCoordinator_ReturnsMedicationsWithPharmacyStock()
    {
        var name = await SeedMedicationAsync(stock: 7);
        await UseTokenAsync(RoleNames.CasaConvivenciaCoordinator);

        var response = await _client.GetAsync("/api/medication-schemes/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = items.EnumerateArray().First(i => i.GetProperty("name").GetString() == name);
        Assert.Equal(7, item.GetProperty("pharmacyStock").GetInt32());
    }

    [Fact]
    public async Task GetCatalog_AsListener_Returns403()
    {
        await UseTokenAsync(RoleNames.Listener);

        var response = await _client.GetAsync("/api/medication-schemes/catalog");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDailyAttendance_AsListener_ReturnsEmptyDayWhenNobodyAttended()
    {
        await UseTokenAsync(RoleNames.Listener);

        var response = await _client.GetAsync("/api/attendances?activity=0&date=2020-01-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var day = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, day.GetProperty("persons").GetArrayLength());
        Assert.Equal(0, day.GetProperty("anonymousCount").GetInt32());
    }

    [Fact]
    public async Task GetMonthlySummaryPdf_AsReferent_ReturnsPdfDocument()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync("/api/attendances/monthly-summary/pdf?year=2026&month=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
