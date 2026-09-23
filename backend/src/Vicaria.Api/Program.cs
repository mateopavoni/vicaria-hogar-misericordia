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

// necesario para que WebApplicationFactory<Program> lo encuentre en los tests de integración
public partial class Program { }
