namespace Vicaria.Infrastructure.Backups;

public class BackupSmtpOptions
{
    public const string SectionName = "Backup:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string AdminEmails { get; set; } = string.Empty;
}
