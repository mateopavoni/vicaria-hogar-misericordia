using Vicaria.Domain.Entities;

namespace Vicaria.Application.Backups;

public record BackupDto(
    Guid Id,
    BackupStatus Status,
    DateTime CreatedAt,
    long SizeBytes,
    string ExternalKey,
    string? ErrorMessage);
