using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Backups;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/admin/backups")]
[Authorize(Roles = RoleNames.Referent)]
public class AdminBackupsController : ControllerBase
{
    private readonly IBackupService _backupService;

    public AdminBackupsController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetBackups([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _backupService.GetPagedAsync(page, pageSize, cancellationToken);

        return result.Error switch
        {
            null => Ok(result.Page),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestBackup(CancellationToken cancellationToken)
    {
        var latest = await _backupService.GetLatestAsync(cancellationToken);
        if (latest is null)
        {
            return NotFound(new { message = "Todavía no se generó ningún respaldo." });
        }

        return Ok(latest);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadBackup(Guid id, CancellationToken cancellationToken)
    {
        var result = await _backupService.GetFileAsync(id, ActorId, cancellationToken);

        if (!result.Success || result.FileStream is null)
        {
            return result.Error switch
            {
                GetBackupFileError.BackupNotFound => NotFound(new { message = result.ErrorMessage }),
                GetBackupFileError.BackupNotAvailable => Conflict(new { message = result.ErrorMessage }),
                GetBackupFileError.FileNotFoundInStorage => NotFound(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        var fileDto = result.FileStream;
        return File(fileDto.Stream, fileDto.ContentType, fileDto.FileName);
    }
}
