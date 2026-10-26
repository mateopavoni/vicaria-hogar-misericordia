namespace Vicaria.Infrastructure.Backups;

public class BackupOptions
{
    public const string SectionName = "Backup";

    public string StorageProvider { get; set; } = "Local";
    public int RetentionCount { get; set; } = 30;
    public int ScheduleHourUtc { get; set; } = 3;
}
