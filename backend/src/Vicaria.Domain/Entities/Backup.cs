namespace Vicaria.Domain.Entities;

public class Backup
{
    public Guid Id { get; set; }
    public BackupStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
