using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Vicaria.Application.Persons;

namespace Vicaria.Api.Filters;

// aplica la restricción de la Directora (solo Residentes) a un endpoint por id de persona
// o de ficha; si no puede acceder responde 404 para no revelar que el recurso existe
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class DirectorResidentsOnlyAttribute : TypeFilterAttribute
{
    public DirectorResidentsOnlyAttribute(string routeKey = "id", bool isSocialRecordId = false)
        : base(typeof(DirectorResidentsOnlyFilter))
    {
        Arguments = [routeKey, isSocialRecordId];
    }
}

public class DirectorResidentsOnlyFilter : IAsyncActionFilter
{
    private readonly IPersonAccessService _accessService;
    private readonly string _routeKey;
    private readonly bool _isSocialRecordId;

    public DirectorResidentsOnlyFilter(IPersonAccessService accessService, string routeKey, bool isSocialRecordId)
    {
        _accessService = accessService;
        _routeKey = routeKey;
        _isSocialRecordId = isSocialRecordId;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var role = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);

        if (context.RouteData.Values.TryGetValue(_routeKey, out var raw) && Guid.TryParse(raw?.ToString(), out var id))
        {
            var allowed = _isSocialRecordId
                ? await _accessService.CanAccessSocialRecordAsync(id, role, context.HttpContext.RequestAborted)
                : await _accessService.CanAccessPersonAsync(id, role, context.HttpContext.RequestAborted);

            if (!allowed)
            {
                context.Result = new NotFoundResult();
                return;
            }
        }

        await next();
    }
}
