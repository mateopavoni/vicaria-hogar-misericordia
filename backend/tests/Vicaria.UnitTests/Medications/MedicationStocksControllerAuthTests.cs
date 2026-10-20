using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Api.Controllers;
using Vicaria.Domain.Entities;

namespace Vicaria.UnitTests.Controllers;

public class MedicationStocksControllerAuthTests
{
    [Fact]
    public void Controller_HasAuthorizeAttribute_WithCorrectRoles()
    {
        var authAttribute = typeof(MedicationStocksController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authAttribute);
        var expectedRoles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector}";
        Assert.Equal(expectedRoles, authAttribute.Roles);
    }

    [Fact]
    public void Controller_HasRouteAttribute_WithCorrectTemplate()
    {
        var routeAttribute = typeof(MedicationStocksController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(routeAttribute);
        Assert.Equal("api/stock-medicamentos", routeAttribute.Template);
    }
}