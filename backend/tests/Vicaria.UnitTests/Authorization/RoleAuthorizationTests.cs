using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Vicaria.Domain.Entities;

namespace Vicaria.UnitTests.Authorization;

public class RoleAuthorizationTests
{
    private static async Task<bool> RolCumpleRequisitoAsync(string rolDelUsuario, params string[] rolesRequeridos)
    {
        var requirement = new RolesAuthorizationRequirement(rolesRequeridos);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, rolDelUsuario)], "Test"));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

        await requirement.HandleAsync(context);

        return context.HasSucceeded;
    }

    [Fact]
    public async Task RolCoincideConElRequerido_Autoriza()
    {
        Assert.True(await RolCumpleRequisitoAsync(RolNombres.DirectoraDeCasona, RolNombres.DirectoraDeCasona));
    }

    [Fact]
    public async Task RolNoCoincideConElRequerido_NoAutoriza()
    {
        Assert.False(await RolCumpleRequisitoAsync(RolNombres.Referente, RolNombres.DirectoraDeCasona));
    }

    [Fact]
    public async Task RolEstaEntreVariosRequeridos_Autoriza()
    {
        Assert.True(await RolCumpleRequisitoAsync(RolNombres.Escucha, RolNombres.DirectoraDeCasona, RolNombres.Escucha));
    }

    [Fact]
    public async Task UsuarioSinClaimDeRol_NoAutoriza()
    {
        var requirement = new RolesAuthorizationRequirement([RolNombres.Referente]);
        var principal = new ClaimsPrincipal(new ClaimsIdentity("Test"));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);

        await requirement.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
