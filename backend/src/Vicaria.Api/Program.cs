using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vicaria.Application.Auth;
using Vicaria.Application.Attendances;
using Vicaria.Application.CasaConvivenciaStays;
using Vicaria.Application.Notifications;
using Vicaria.Application.Persons;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Attendances;
using Vicaria.Infrastructure.Auth;
using Vicaria.Infrastructure.CasaConvivenciaStays;
using Vicaria.Infrastructure.Notifications;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.SocialRecords;
using Vicaria.Infrastructure.Persons;
using Vicaria.Application.Observations;
using Vicaria.Infrastructure.Observations;
using Vicaria.Application.LifeStories;
using Vicaria.Infrastructure.LifeStories;
using Vicaria.Application.Timelines;
using Vicaria.Infrastructure.Timelines;

var builder = WebApplication.CreateBuilder(args);

// secretos locales (connection string real, etc.), no versionado
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, [] } });
});

builder.Services.AddDbContext<VicariaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VicariaDb")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISocialRecordService, SocialRecordService>();
builder.Services.AddScoped<ICasaConvivenciaStayService, CasaConvivenciaStayService>();
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
builder.Services.AddScoped<IValidator<ApproveUserDto>, ApproveUserDtoValidator>();
builder.Services.AddScoped<IValidator<RejectUserDto>, RejectUserDtoValidator>();
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<IValidator<RefreshTokenDto>, RefreshTokenDtoValidator>();
builder.Services.AddScoped<IValidator<CreateSocialRecordDto>, CreateSocialRecordDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateSocialRecordDto>, UpdateSocialRecordDtoValidator>();
builder.Services.AddScoped<IValidator<UpdatePersonTypeDto>, UpdatePersonTypeDtoValidator>();
builder.Services.AddScoped<IValidator<UpdatePersonProfileStatusDto>, UpdatePersonProfileStatusValidator>();
builder.Services.AddScoped<IPersonInactivityService, PersonInactivityService>();
builder.Services.AddHostedService<InactivityBackgroundService>();
builder.Services.AddScoped<IValidator<CasaConvivenciaStayExitDto>, CasaConvivenciaStayExitDtoValidator>();
builder.Services.AddScoped<IValidator<CreateAttendanceDto>, CreateAttendanceDtoValidator>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceInactivityService, AttendanceInactivityService>();
builder.Services.AddHostedService<AttendanceInactivityBackgroundService>();
builder.Services.AddScoped<IObservationService, ObservationService>();
builder.Services.AddScoped<IValidator<CreateObservationDto>, CreateObservationDtoValidator>();
builder.Services.AddScoped<IObservationCategoryService, ObservationCategoryService>();
builder.Services.AddScoped<IValidator<CreateObservationCategoryDto>, CreateObservationCategoryDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateObservationCategoryDto>, UpdateObservationCategoryDtoValidator>();
builder.Services.AddScoped<ILifeStoryService, LifeStoryService>();
builder.Services.AddScoped<IValidator<UpdateLifeStoryStageDto>, UpdateLifeStoryStageDtoValidator>();
builder.Services.AddScoped<IProfileTimelineService, ProfileTimelineService>();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // leemos Jwt:Key acá adentro (no afuera, antes del builder.Build()) porque en los
        // tests de integración la config de test se agrega recién al armar el host;
        // leerla antes usaba la key de appsettings en vez de la de test
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Falta configurar Jwt:Key (variable de entorno Jwt__Key). La API no puede arrancar sin esto.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };

        // si cambia el rol o se desactiva la cuenta, TokenVersion sube y el token viejo deja de servir
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenVersionClaim = context.Principal?.FindFirstValue("token_version");
                if (userIdClaim is null || tokenVersionClaim is null)
                {
                    context.Fail("Token inválido.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<VicariaDbContext>();
                var user = await dbContext.Users.FindAsync(Guid.Parse(userIdClaim));
                if (user is null || user.Status != UserStatus.Active || user.TokenVersion.ToString() != tokenVersionClaim)
                {
                    context.Fail("Token inválido.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// CORS: el front vive en otro origen (otro puerto en dev, otro dominio en la VPS),
// los orígenes permitidos se configuran por ambiente en appsettings, no van hardcodeados
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// aplica las migraciones pendientes solas al arrancar, para no depender de correr
// "dotnet ef database update" a mano en la VPS
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
    dbContext.Database.Migrate();

    // usuarios de prueba con contraseña conocida, uno por rol, para QA manual local (docker-compose)
    if (app.Environment.IsDevelopment())
    {
        SeedTestUsers(dbContext);
        SeedDemoData(dbContext);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void SeedTestUsers(VicariaDbContext dbContext)
{
    var testUsers = new[]
    {
        (Email: "referente@test.com", RoleId: new Guid("11111111-1111-1111-1111-111111111111")),
        (Email: "directora@test.com", RoleId: new Guid("22222222-2222-2222-2222-222222222222")),
        (Email: "escucha@test.com", RoleId: new Guid("33333333-3333-3333-3333-333333333333")),
        (Email: "coordinador@test.com", RoleId: new Guid("77777777-7777-7777-7777-777777777777")),
    };

    foreach (var (email, roleId) in testUsers)
    {
        if (dbContext.Users.Any(u => u.Email == email))
        {
            continue;
        }

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = email.Split('@')[0],
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!"),
            Status = UserStatus.Active,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow
        });
    }

    dbContext.SaveChanges();
}

// datos de ejemplo (personas ambulatorias/residentes, estadías, observaciones) para QA manual
// local y demos (docker-compose) — idempotente: si ya hay alguna Person, no vuelve a sembrar
static void SeedDemoData(VicariaDbContext dbContext)
{
    if (dbContext.People.Any())
    {
        return;
    }

    var referente = dbContext.Users.FirstOrDefault(u => u.Email == "referente@test.com");
    var escucha = dbContext.Users.FirstOrDefault(u => u.Email == "escucha@test.com");
    var directora = dbContext.Users.FirstOrDefault(u => u.Email == "directora@test.com");
    if (referente is null || escucha is null || directora is null)
    {
        // sin los usuarios de prueba no hay a quién asignarle CreatedByUserId/AuthorUserId
        return;
    }

    var now = DateTime.UtcNow;

    (Person Person, SocialRecord Record) MakePerson(
        string firstName, string lastName, string? dni, PersonType personType, SocialRecordStatus status,
        string reasonForEntry, string? housingSituation, string? occupation, int entryDaysAgo)
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Dni = dni,
            CreatedAt = now.AddDays(-entryDaysAgo)
        };
        var record = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = status,
            PersonType = personType,
            ReasonForEntry = reasonForEntry,
            EntryDate = now.AddDays(-entryDaysAgo),
            HousingSituation = housingSituation,
            Occupation = occupation,
            HasDocumentation = dni is not null,
            CreatedByUserId = referente.Id,
            CreatedAt = now.AddDays(-entryDaysAgo),
            UpdatedAt = now.AddDays(-entryDaysAgo)
        };
        return (person, record);
    }

    var (personJuan, recordJuan) = MakePerson(
        "Juan", "Pérez", "30123456", PersonType.Ambulatory, SocialRecordStatus.Active,
        "Situación de calle, se acerca al Centro Barrial por el comedor", "Calle", "Changas", 25);
    var (personMaria, recordMaria) = MakePerson(
        "María", "Gómez", "28987654", PersonType.Ambulatory, SocialRecordStatus.Active,
        "Busca acompañamiento y apoyo alimentario", "Pensión", "Desocupada", 15);
    var (personCarlos, recordCarlos) = MakePerson(
        "Carlos", "Rodríguez", "25456789", PersonType.Resident, SocialRecordStatus.Active,
        "Ingresa a la Casa de Convivencia derivado por el equipo de calle", "Sin vivienda", null, 20);
    var (personLucia, recordLucia) = MakePerson(
        "Lucía", "Fernández", null, PersonType.Resident, SocialRecordStatus.Active,
        "Ingreso voluntario a la Casa de Convivencia", "Sin vivienda", null, 10);
    var (personRoberto, recordRoberto) = MakePerson(
        "Roberto", "Sánchez", "22334455", PersonType.Resident, SocialRecordStatus.Inactive,
        "Estadía finalizada, alta del equipo", "Sin vivienda", "Changas", 90);
    var (personAna, recordAna) = MakePerson(
        "Ana", "Torres", null, PersonType.Ambulatory, SocialRecordStatus.Inactive,
        "Dejó de asistir al Centro Barrial", "Familiar", null, 120);

    dbContext.People.AddRange(personJuan, personMaria, personCarlos, personLucia, personRoberto, personAna);
    dbContext.SocialRecords.AddRange(recordJuan, recordMaria, recordCarlos, recordLucia, recordRoberto, recordAna);

    dbContext.Contacts.AddRange(
        new Contact
        {
            Id = Guid.NewGuid(),
            SocialRecordId = recordJuan.Id,
            FirstName = "Marta",
            LastName = "Pérez",
            Phone = "1145678901",
            Address = "Av. Rivadavia 1234, CABA"
        },
        new Contact
        {
            Id = Guid.NewGuid(),
            SocialRecordId = recordMaria.Id,
            FirstName = "Pedro",
            LastName = "Gómez",
            Phone = "1156789012"
        }
    );

    // evaluación psiquiátrica vigente para los residentes (Carlos, Lucía) — condición para PersonType.Resident (SCRUM-134)
    dbContext.PsychiatricEvaluations.AddRange(
        new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = personCarlos.Id,
            Date = now.AddDays(-20),
            Professional = "Dra. Silvina López",
            Diagnosis = "Apto para convivencia asistida",
            IsValid = true,
            RegisteredByUserId = directora.Id
        },
        new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = personLucia.Id,
            Date = now.AddDays(-10),
            Professional = "Dr. Fernando Castro",
            Diagnosis = "Apto para convivencia asistida",
            IsValid = true,
            RegisteredByUserId = directora.Id
        },
        new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = personRoberto.Id,
            Date = now.AddDays(-90),
            Professional = "Dra. Silvina López",
            Diagnosis = "Apto para convivencia asistida",
            IsValid = false,
            RegisteredByUserId = directora.Id
        }
    );

    // estadías en la Casa de Convivencia: dos abiertas (residentes activos), una cerrada (Roberto)
    dbContext.CasaConvivenciaStays.AddRange(
        new CasaConvivenciaStay
        {
            Id = Guid.NewGuid(),
            PersonId = personCarlos.Id,
            EntryDate = now.AddDays(-20)
        },
        new CasaConvivenciaStay
        {
            Id = Guid.NewGuid(),
            PersonId = personLucia.Id,
            EntryDate = now.AddDays(-10)
        },
        new CasaConvivenciaStay
        {
            Id = Guid.NewGuid(),
            PersonId = personRoberto.Id,
            EntryDate = now.AddDays(-90),
            ExitDate = now.AddDays(-5),
            ExitReason = StayExitReason.TeamDischarge
        }
    );

    // ids fijos de ObservationCategoryConfiguration (Salud, Documentación, Situación habitacional, ...)
    var categoriaSalud = Guid.Parse("11111111-1111-1111-1111-111111111101");
    var categoriaDocumentacion = Guid.Parse("11111111-1111-1111-1111-111111111102");
    var categoriaHabitacional = Guid.Parse("11111111-1111-1111-1111-111111111103");
    var categoriaGeneral = Guid.Parse("11111111-1111-1111-1111-111111111106");

    dbContext.Observations.AddRange(
        new Observation
        {
            Id = Guid.NewGuid(),
            PersonId = personJuan.Id,
            Content = "Se acercó al Centro Barrial, buenas condiciones generales de salud. Se le ofreció turno médico.",
            CategoryId = categoriaSalud,
            AuthorUserId = escucha.Id,
            CreatedAt = now.AddDays(-40)
        },
        new Observation
        {
            Id = Guid.NewGuid(),
            PersonId = personJuan.Id,
            Content = "Inició trámite de renovación de DNI con acompañamiento del referente.",
            CategoryId = categoriaDocumentacion,
            AuthorUserId = referente.Id,
            CreatedAt = now.AddDays(-15)
        },
        new Observation
        {
            Id = Guid.NewGuid(),
            PersonId = personMaria.Id,
            Content = "Consultó por posibilidad de alojamiento transitorio, se evaluará derivación.",
            CategoryId = categoriaHabitacional,
            AuthorUserId = escucha.Id,
            CreatedAt = now.AddDays(-25)
        },
        new Observation
        {
            Id = Guid.NewGuid(),
            PersonId = personCarlos.Id,
            Content = "Buena adaptación a la convivencia durante la primera semana.",
            CategoryId = categoriaGeneral,
            AuthorUserId = directora.Id,
            CreatedAt = now.AddDays(-13)
        },
        new Observation
        {
            Id = Guid.NewGuid(),
            PersonId = personLucia.Id,
            Content = "Participó activamente de las actividades grupales de la semana.",
            CategoryId = categoriaGeneral,
            AuthorUserId = directora.Id,
            CreatedAt = now.AddDays(-3)
        }
    );

    // asistencia reciente para los 4 activos: el job de inactividad (SCRUM-135) pasa a
    // Inactive cualquier ficha Activa sin asistencia en los últimos 30 días, sin importar
    // qué tan reciente sea el ingreso — sin esto, la demo arranca con todo marcado inactivo
    dbContext.Attendances.AddRange(
        new Attendance { Id = Guid.NewGuid(), PersonId = personJuan.Id, Date = now.AddDays(-2), CreatedByUserId = referente.Id },
        new Attendance { Id = Guid.NewGuid(), PersonId = personMaria.Id, Date = now.AddDays(-5), CreatedByUserId = escucha.Id },
        new Attendance { Id = Guid.NewGuid(), PersonId = personCarlos.Id, Date = now.AddDays(-1), CreatedByUserId = directora.Id },
        new Attendance { Id = Guid.NewGuid(), PersonId = personLucia.Id, Date = now, CreatedByUserId = directora.Id }
    );

    dbContext.SaveChanges();
}

// necesario para que WebApplicationFactory<Program> lo encuentre en los tests de integración
public partial class Program { }
