using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Collaborators;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.IntegrationTests.Auth;
using Xunit;

namespace Vicaria.IntegrationTests.Collaborators;

public class CollaboratorsEndpointTests : IClassFixture<VicariaWebApplicationFactory>
{
    private const string Endpoint = "/api/collaborators";

    private readonly VicariaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CollaboratorsEndpointTests(VicariaWebApplicationFactory factory)
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
            new AuthenticationHeaderValue("Bearer", TestJwtFactory.CreateToken("Test", "test@mail.com", role, actorId));
        return actorId;
    }

    // DNI distinto en cada payload: la BD de test es compartida entre los tests de la clase
    // y el DNI es único (índice condicional de SCRUM-199)
    private static string NewDni() => Random.Shared.Next(10000000, 99999999).ToString();

    private static object ValidPayload(
        string firstName = "María",
        string? lastName = "Pérez",
        string? dni = null,
        string? phone = "1145678901",
        string? email = "maria@mail.com",
        int type = 0,
        string? workArea = "Comedor") => new
    {
        firstName,
        lastName,
        dni = dni ?? NewDni(),
        phone,
        email,
        type,
        workArea
    };

    [Fact]
    public async Task Post_WithReferente_Returns201WithId()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync(Endpoint, ValidPayload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body["id"]);
    }

    [Theory]
    [InlineData(RoleNames.CasaConvivenciaDirector)]
    [InlineData(RoleNames.CasaConvivenciaCoordinator)]
    [InlineData(RoleNames.Listener)]
    public async Task Post_WithUnauthorizedRole_Returns403(string role)
    {
        await UseTokenAsync(role);

        var response = await _client.PostAsJsonAsync(Endpoint, ValidPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(Endpoint, ValidPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithMissingFirstName_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync(Endpoint, ValidPayload(firstName: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("nombre", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_WithTooLongFirstName_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync(Endpoint, ValidPayload(firstName: new string('a', 101)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("100 caracteres", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_WithInvalidEmail_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.PostAsJsonAsync(Endpoint, ValidPayload(email: "no-es-un-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("email", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_WithNullOptionalFields_Returns201()
    {
        await UseTokenAsync(RoleNames.Referent);

        var payload = new
        {
            firstName = "Sin",
            lastName = (string?)null,
            dni = (string?)null,
            phone = (string?)null,
            email = (string?)null,
            type = 1,
            workArea = (string?)null
        };
        var response = await _client.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithDuplicateDni_Returns409()
    {
        var actorId = await UseTokenAsync(RoleNames.Referent);
        await SeedCollaboratorAsync(actorId, dni: "30123456");

        var response = await _client.PostAsJsonAsync(Endpoint, ValidPayload(dni: "30.123.456"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("DNI", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_PersistsCollaboratorWithActorAndWritesAuditLog()
    {
        var actorId = await UseTokenAsync(RoleNames.Referent);
        var dni = NewDni();

        var response = await _client.PostAsJsonAsync(Endpoint, ValidPayload(dni: dni));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var id = body!["id"];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var stored = db.Collaborators.Single(c => c.Id == id);
        Assert.Equal("María", stored.FirstName);
        Assert.Equal("Pérez", stored.LastName);
        Assert.Equal(dni, stored.Dni);
        Assert.Equal(CollaboratorType.Volunteer, stored.Type);
        Assert.Equal(actorId, stored.RegisteredByUserId);
        Assert.True(stored.RegisteredAt <= DateTime.UtcNow);

        var log = db.AuditLogs.Single(l => l.AffectedEntity == $"Collaborator:{id}");
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Colaborador creado", log.Action);
    }

    [Fact]
    public async Task Search_WithToken_Returns200WithMatch()
    {
        await UseTokenAsync(RoleNames.Referent);
        var unique = Guid.NewGuid().ToString("N");
        await SeedSearchCollaboratorAsync(firstName: $"Busca{unique}", lastName: "Gómez");

        var response = await _client.GetAsync($"{Endpoint}/search?q=busca{unique}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>();
        var result = Assert.Single(results!);
        Assert.Equal($"Busca{unique} Gómez", result.FullName);
        Assert.Equal(CollaboratorType.Volunteer, result.Type);
    }

    [Fact]
    public async Task Search_IgnoresAccentsAndCase()
    {
        await UseTokenAsync(RoleNames.Referent);
        var unique = Guid.NewGuid().ToString("N");
        await SeedSearchCollaboratorAsync(firstName: $"Joaquín{unique}");

        var response = await _client.GetAsync($"{Endpoint}/search?q=JOAQUIN{unique}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>();
        Assert.Single(results!);
    }

    [Fact]
    public async Task Search_ByLastNameWithoutAccents_FindsCollaborator()
    {
        await UseTokenAsync(RoleNames.Referent);
        var unique = Guid.NewGuid().ToString("N");
        await SeedSearchCollaboratorAsync(firstName: $"Apellido{unique}", lastName: "Pérez");

        var response = await _client.GetAsync($"{Endpoint}/search?q=perez");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>();
        Assert.Contains(results!, r => r.FullName == $"Apellido{unique} Pérez");
    }

    [Fact]
    public async Task Search_ByWorkArea_FindsCollaborator()
    {
        await UseTokenAsync(RoleNames.Referent);
        var unique = Guid.NewGuid().ToString("N");
        await SeedSearchCollaboratorAsync(firstName: $"Area{unique}", workArea: $"Comedor{unique}");

        var response = await _client.GetAsync($"{Endpoint}/search?q=comedor{unique}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>();
        var result = Assert.Single(results!);
        Assert.Equal($"Comedor{unique}", result.WorkArea);
    }

    [Fact]
    public async Task Search_WithTypeFilter_ReturnsOnlyThatType()
    {
        await UseTokenAsync(RoleNames.Referent);
        var unique = Guid.NewGuid().ToString("N");
        await SeedSearchCollaboratorAsync(firstName: $"Tipo{unique}A", type: CollaboratorType.Volunteer);
        await SeedSearchCollaboratorAsync(firstName: $"Tipo{unique}B", type: CollaboratorType.Employee);

        var volunteers = await _client.GetAsync($"{Endpoint}/search?q=tipo{unique}&type=0");
        var employees = await _client.GetAsync($"{Endpoint}/search?q=tipo{unique}&type=1");

        var volunteerResults = await volunteers.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>();
        var employeeResults = await employees.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>();
        Assert.Equal(CollaboratorType.Volunteer, Assert.Single(volunteerResults!).Type);
        Assert.Equal(CollaboratorType.Employee, Assert.Single(employeeResults!).Type);
    }

    [Fact]
    public async Task Search_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"{Endpoint}/search?q=ana");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithEmptyOrNullQuery_Returns200WithEmptyList()
    {
        await UseTokenAsync(RoleNames.Referent);

        var empty = await _client.GetAsync($"{Endpoint}/search?q=");
        var absent = await _client.GetAsync($"{Endpoint}/search");

        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal(HttpStatusCode.OK, absent.StatusCode);
        Assert.Empty((await empty.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>())!);
        Assert.Empty((await absent.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>())!);
    }

    [Fact]
    public async Task Search_WithTooLongQuery_Returns400WithSpanishMessage()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync($"{Endpoint}/search?q={new string('a', 101)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("100 caracteres", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Search_WithInvalidTypeValue_Returns400()
    {
        await UseTokenAsync(RoleNames.Referent);

        var response = await _client.GetAsync($"{Endpoint}/search?q=ana&type=voluntario");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithNullLastName_ComposesFullNameWithFirstNameOnly()
    {
        await UseTokenAsync(RoleNames.Referent);
        var unique = Guid.NewGuid().ToString("N");
        await SeedSearchCollaboratorAsync(firstName: $"Sinapellido{unique}", lastName: null);

        var response = await _client.GetAsync($"{Endpoint}/search?q=sinapellido{unique}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>();
        var result = Assert.Single(results!);
        Assert.Equal($"Sinapellido{unique}", result.FullName);
    }

    [Fact]
    public async Task Search_WithPercentWildcard_MatchesOnlyLiteralPercent()
    {
        await UseTokenAsync(RoleNames.Referent);
        var unique = Guid.NewGuid().ToString("N");
        var literal = await SeedSearchCollaboratorAsync(firstName: $"Pct{unique}%Lit");

        var response = await _client.GetAsync($"{Endpoint}/search?q=%25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<CollaboratorSearchResultDto>>();
        var result = Assert.Single(results!);
        Assert.Equal(literal.Id, result.Id);
    }

    private async Task<Collaborator> SeedSearchCollaboratorAsync(
        string firstName,
        string? lastName = "Test",
        CollaboratorType type = CollaboratorType.Volunteer,
        string? workArea = null)
    {
        var actorId = await SeedActorAsync(RoleNames.Referent);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Type = type,
            WorkArea = workArea,
            RegisteredByUserId = actorId,
            RegisteredAt = DateTime.UtcNow
        };
        db.Collaborators.Add(collaborator);
        await db.SaveChangesAsync();
        return collaborator;
    }

    private async Task SeedCollaboratorAsync(Guid registeredByUserId, string? dni)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
        db.Collaborators.Add(new Collaborator
        {
            Id = Guid.NewGuid(),
            FirstName = "Existente",
            Dni = dni,
            Type = CollaboratorType.Volunteer,
            RegisteredByUserId = registeredByUserId,
            RegisteredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
