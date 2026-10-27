using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Vicaria.Application.Backups;

namespace Vicaria.Infrastructure.Backups;

public class SmtpBackupEmailService : IBackupEmailService
{
    private readonly BackupSmtpOptions _options;

    public SmtpBackupEmailService(IOptions<BackupSmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendBackupFailureAsync(string errorMessage, DateTime occurredAt, CancellationToken cancellationToken = default)
    {
        var recipients = _options.AdminEmails
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (recipients.Length == 0)
        {
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(_options.From) ? _options.User : _options.From),
            Subject = "Vicaria: falló el respaldo automático de la base de datos",
            Body = $"El respaldo automático del {occurredAt:yyyy-MM-dd HH:mm} UTC falló.\n\nDetalle: {errorMessage}\n\nRevise el panel de respaldos para más información.",
            IsBodyHtml = false
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(recipient);
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.User, _options.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
