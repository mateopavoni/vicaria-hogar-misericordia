using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Vicaria.Api.Controllers;
using Vicaria.Domain.Entities;

namespace Vicaria.UnitTests.CasonaVisits;

public class CasonaVisitsAuthorizationTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute_RestrictedToExpectedRoles()
    {
        var authAttribute = typeof(CasonaVisitsController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttribute);

        var roles = authAttribute.Roles?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        Assert.Contains(RoleNames.Referent, roles);
        Assert.Contains(RoleNames.CasaConvivenciaDirector, roles);
        Assert.Contains(RoleNames.CasaConvivenciaCoordinator, roles);
        Assert.DoesNotContain(RoleNames.Listener, roles);
    }

    [Fact]
    public void GetByRange_DoesNotAllowListenerRole()
    {
        var method = typeof(CasonaVisitsController).GetMethod(nameof(CasonaVisitsController.GetByRange));
        Assert.NotNull(method);

        var methodAuth = method.GetCustomAttribute<AuthorizeAttribute>();

        if (methodAuth?.Roles is not null)
        {
            var methodRoles = methodAuth.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.DoesNotContain(RoleNames.Listener, methodRoles);
        }

        var allowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.Null(allowAnonymous);
    }
}