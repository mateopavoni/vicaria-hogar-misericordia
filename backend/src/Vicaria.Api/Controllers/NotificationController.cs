using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Notifications;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(role))
        {
            return Forbid();
        }

        var notifications = await _notificationService.GetByRoleAsync(role, cancellationToken);
        return Ok(notifications);
    }

    [HttpPatch("{id}/read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsReadAsync(id, ActorId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }
        return NoContent();
    }

    [HttpPatch("read-all")]
    [Authorize]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(role))
        {
            return Forbid();
        }

        await _notificationService.MarkAllAsReadAsync(role, cancellationToken);
        return NoContent();
    }
}
