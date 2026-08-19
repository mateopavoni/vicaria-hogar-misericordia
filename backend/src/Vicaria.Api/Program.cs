using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Auth;
using Vicaria.Infrastructure.Auth;
using Vicaria.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// secretos locales (connection string real, etc.), no versionado
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<VicariaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("VicariaDb")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// necesario para que WebApplicationFactory<Program> lo encuentre en los tests de integración
public partial class Program { }
