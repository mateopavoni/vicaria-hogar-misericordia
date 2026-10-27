namespace Vicaria.Infrastructure.Backups;

public class S3BackupOptions
{
    public const string SectionName = "Backup:S3";

    public string ServiceUrl { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UsePathStyle { get; set; } = true;
}
