using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Api.Controllers;
using Vicaria.Domain.Entities;

namespace Vicaria.UnitTests.Controllers;

public class MedicationsAgendaControllerAuthTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute_WithCorrectRoles()
    {
        var authAttribute = typeof(MedicationsAgendaController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttribute);
        var expectedRoles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}";
        Assert.Equal(expectedRoles, authAttribute.Roles);
    }

    [Fact]
    public void Controller_HasRouteAttribute_WithCorrectTemplate()
    {
        var routeAttribute = typeof(MedicationsAgendaController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(routeAttribute);
        Assert.Equal("api/medicamentos/agenda", routeAttribute.Template);
    }
}