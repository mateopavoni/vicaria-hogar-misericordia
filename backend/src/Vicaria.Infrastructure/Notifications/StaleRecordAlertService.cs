using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Notifications;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Notifications;

public class StaleRecordAlertService : IStaleRecordAlertService
{
    public const string EventType = "FichaSinObservaciones";
    private const int ThresholdDays = 30;

    private readonly VicariaDbContext _dbContext;

    public StaleRecordAlertService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CheckAndNotifyAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddDays(-ThresholdDays);

        // sin observaciones recientes: la última observación (o, si no tiene ninguna, la creación de la ficha) es anterior al umbral
        var staleRecords = await _dbContext.SocialRecords
            .Include(r => r.Person)
            .Where(r => r.Status == SocialRecordStatus.Active
                && r.CreatedAt < threshold
                && !_dbContext.Observations.Any(o => o.PersonId == r.PersonId && o.CreatedAt >= threshold))
            .ToListAsync(cancellationToken);

        if (staleRecords.Count == 0)
        {
            return 0;
        }

        var links = staleRecords.Select(r => LinkFor(r.Id)).ToList();

        // no repetir la alerta si ya se generó en la ventana actual de 30 días
        var alreadyNotified = (await _dbContext.Notifications
            .Where(n => n.EventType == EventType && n.CreatedAt >= threshold && links.Contains(n.LinkUrl!))
            .Select(n => n.LinkUrl!)
            .ToListAsync(cancellationToken)).ToHashSet();

        var created = 0;
        foreach (var record in staleRecords)
        {
            var link = LinkFor(record.Id);
            if (alreadyNotified.Contains(link))
            {
                continue;
            }

            var name = record.Person is null ? "Una ficha" : $"La ficha de {record.Person.FirstName} {record.Person.LastName}".Trim();
            _dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Description = $"{name} lleva más de {ThresholdDays} días sin observaciones.",
                EventType = EventType,
                LinkUrl = link,
                IsRead = false,
                CreatedAt = now,
                TargetRole = RoleNames.Referent
            });
            created++;
        }

        if (created > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    private static string LinkFor(Guid recordId) => $"/dashboard/fichas/{recordId}";
}
