using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Vicaria.IntegrationTests.Auth;

// firma un JWT igual al que emitirá el login real (SCRUM-83), usando la misma
// clave de test que VicariaWebApplicationFactory le da a la app
public static class TestJwtFactory
{
    public static string CrearToken(string nombre, string email, string rol, Guid? usuarioId = null)
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, (usuarioId ?? Guid.NewGuid()).ToString()),
            new Claim(ClaimTypes.Name, nombre),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, rol)
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(VicariaWebApplicationFactory.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: VicariaWebApplicationFactory.JwtIssuer,
            audience: VicariaWebApplicationFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
