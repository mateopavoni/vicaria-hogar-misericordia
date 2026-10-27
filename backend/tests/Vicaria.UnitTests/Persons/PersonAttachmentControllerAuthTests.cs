using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Api.Controllers;
using Vicaria.Domain.Entities;

namespace Vicaria.UnitTests.Persons;

public class PersonAttachmentControllerAuthTests
{
    [Fact]
    public void DeleteAttachment_RequiresReferentRoleOnly()
    {
        var method = typeof(PersonsController).GetMethod(nameof(PersonsController.DeleteAttachment));

        Assert.NotNull(method);
        var authAttribute = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttribute);
        Assert.Equal(RoleNames.Referent, authAttribute.Roles);
    }
}