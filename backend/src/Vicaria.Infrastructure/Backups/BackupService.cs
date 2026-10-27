using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vicaria.Application.Backups;
using Vicaria.Application.Common;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Backups;

public class BackupService : IBackupService
{
    public const string FailureEventType = "RespaldoFallido";

    private readonly VicariaDbContext _dbContext;
    private readonly IBackupStorageService _storageService;
    private readonly IBackupEmailService _emailService;
    private readonly BackupOptions _options;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        VicariaDbContext dbContext,
        IBackupStorageService storageService,
        IBackupEmailService emailService,
        IOptions<BackupOptions> options,
        ILogger<BackupService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _emailService = emailService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GetBackupsResult> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1)
        {
            return GetBackupsResult.InvalidPage();
        }

        var query = _dbContext.Backups.OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BackupDto(b.Id, b.Status, b.CreatedAt, b.SizeBytes, b.ExternalKey, b.ErrorMessage))
            .ToListAsync(cancellationToken);

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return GetBackupsResult.Ok(new PagedResult<BackupDto>(items, total, totalPages));
    }

    public async Task<BackupDto?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var backup = await _dbContext.Backups
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return backup is null
            ? null
            : new BackupDto(backup.Id, backup.Status, backup.CreatedAt, backup.SizeBytes, backup.ExternalKey, backup.ErrorMessage);
    }

    public async Task<GetBackupFileResult> GetFileAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var backup = await _dbContext.Backups.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (backup is null)
        {
            return GetBackupFileResult.BackupNotFound();
        }

        if (backup.Status != BackupStatus.Succeeded)
        {
            return GetBackupFileResult.BackupNotAvailable();
        }

        var stream = await _storageService.DownloadAsync(backup.ExternalKey, cancellationToken);
        if (stream is null)
        {
            return GetBackupFileResult.FileNotFoundInStorage();
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "DescargarRespaldo",
            AffectedEntity = $"Backup:{backup.Id}",
            Date = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        var fileName = $"respaldo-{backup.CreatedAt:yyyyMMdd-HHmm}.bak";
        return GetBackupFileResult.Ok(new BackupFileStreamDto(stream, "application/octet-stream", fileName));
    }

    public async Task<BackupDto> CreateDailyBackupAsync(CancellationToken cancellationToken = default)
    {
        var occurredAt = DateTime.UtcNow;

        var backup = new Backup
        {
            Id = Guid.NewGuid(),
            Status = BackupStatus.InProgress,
            CreatedAt = occurredAt,
            SizeBytes = 0,
            ExternalKey = string.Empty
        };
        _dbContext.Backups.Add(backup);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"vicaria-backup-{backup.Id:N}.bak");
            try
            {
                await GenerateDatabaseBackupAsync(tempPath, cancellationToken);

                await using var fileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var stored = await _storageService.UploadAsync(fileStream, "respaldo.bak", cancellationToken);

                backup.Status = BackupStatus.Succeeded;
                backup.SizeBytes = stored.SizeBytes;
                backup.ExternalKey = stored.ExternalKey;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
        catch (Exception ex)
        {
            backup.Status = BackupStatus.Failed;
            backup.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            _logger.LogError(ex, "Falló la generación del respaldo automático {BackupId}.", backup.Id);
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty,
            Action = backup.Status == BackupStatus.Succeeded ? "GenerarRespaldo" : "FalloRespaldo",
            AffectedEntity = $"Backup:{backup.Id}",
            Date = DateTime.UtcNow
        });

        if (backup.Status == BackupStatus.Failed)
        {
            _dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Description = $"El respaldo automático del {occurredAt:yyyy-MM-dd HH:mm} UTC falló: {backup.ErrorMessage}",
                EventType = FailureEventType,
                LinkUrl = "/configuracion/respaldos",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                TargetRole = RoleNames.Referent
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await NotifyFailureAsync(backup.ErrorMessage ?? "Error desconocido.", occurredAt, cancellationToken);
        }
        else
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await ApplyRetentionAsync(cancellationToken);
        }

        return new BackupDto(backup.Id, backup.Status, backup.CreatedAt, backup.SizeBytes, backup.ExternalKey, backup.ErrorMessage);
    }

    // BACKUP DATABASE escribe el archivo en el disco del SQL Server, que en Docker (y en
    // cualquier deploy real) no es el de la API: se respalda en la carpeta de respaldos
    // de la instancia y los bytes se traen por la misma conexión con OPENROWSET(BULK).
    // Express no admite COMPRESSION, así que solo se comprime en las otras ediciones.
    private const string ServerStagingFileName = "vicaria-backup-staging.bak";
    private const int ExpressEngineEdition = 4;

    private async Task GenerateDatabaseBackupAsync(string localPath, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var databaseName = connection.Database;

        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            string serverBackupDirectory;
            int engineEdition;
            await using (var info = connection.CreateCommand())
            {
                info.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000)), CAST(SERVERPROPERTY('EngineEdition') AS int)";
                await using var reader = await info.ExecuteReaderAsync(cancellationToken);
                await reader.ReadAsync(cancellationToken);
                serverBackupDirectory = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                engineEdition = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }

            if (string.IsNullOrWhiteSpace(serverBackupDirectory))
            {
                throw new InvalidOperationException("El SQL Server no informa una carpeta de respaldos (InstanceDefaultBackupPath).");
            }

            var separator = serverBackupDirectory.Contains('\\') ? '\\' : '/';
            var serverPath = serverBackupDirectory.TrimEnd('/', '\\') + separator + ServerStagingFileName;
            var serverPathLiteral = serverPath.Replace("'", "''");

            await using (var backupCommand = connection.CreateCommand())
            {
                var options = engineEdition == ExpressEngineEdition ? "INIT" : "INIT, COMPRESSION";
                backupCommand.CommandText = $"BACKUP DATABASE [{databaseName.Replace("]", "]]")}] TO DISK = N'{serverPathLiteral}' WITH {options}";
                backupCommand.CommandTimeout = 600;
                await backupCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var readCommand = connection.CreateCommand())
            {
                // OPENROWSET(BULK) no acepta parámetros: la ruta sale de SERVERPROPERTY, no del usuario
                readCommand.CommandText = $"SELECT BulkColumn FROM OPENROWSET(BULK N'{serverPathLiteral}', SINGLE_BLOB) AS backupFile";
                readCommand.CommandTimeout = 600;
                await using var reader = await readCommand.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess, cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException("No se pudo leer el archivo del respaldo generado.");
                }

                await using var source = reader.GetStream(0);
                await using var target = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await source.CopyToAsync(target, cancellationToken);
            }
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task ApplyRetentionAsync(CancellationToken cancellationToken)
    {
        var retentionCount = Math.Max(1, _options.RetentionCount);

        var outdated = await _dbContext.Backups
            .Where(b => b.Status == BackupStatus.Succeeded)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(retentionCount)
            .ToListAsync(cancellationToken);

        foreach (var old in outdated)
        {
            try
            {
                await _storageService.DeleteAsync(old.ExternalKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el archivo externo del respaldo {BackupId}; se conserva la fila.", old.Id);
                continue;
            }

            _dbContext.Backups.Remove(old);
            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Empty,
                Action = "EliminarRespaldoPorRetencion",
                AffectedEntity = $"Backup:{old.Id}",
                Date = DateTime.UtcNow
            });
        }

        if (outdated.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task NotifyFailureAsync(string errorMessage, DateTime occurredAt, CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendBackupFailureAsync(errorMessage, occurredAt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el email de falla del respaldo.");
        }
    }
}
