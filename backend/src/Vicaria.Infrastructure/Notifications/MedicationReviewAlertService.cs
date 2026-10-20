using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Notifications;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.MedicationSchemes;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Notifications;

// alertas de revisión del esquema de medicación (SCRUM-249):
// - "más de 30 días sin revisión" para cada residente con algún esquema vigente vencido
// - recordatorio el día 1 de cada mes para cada residente con esquemas vigentes
// Genera una notificación por cada rol destinatario (Referente/Directora/Coordinador) y
// no repite la del mismo evento dentro de su ventana (30 días, o el mes corriente).
public class MedicationReviewAlertService : IMedicationReviewAlertService
{
    public const string StaleEventType = "EsquemaMedicacionSinRevision";
    public const string MonthlyEventType = "EsquemaMedicacionRevisionMensual";
    private const int ThresholdDays = 30;

    private static readonly string[] TargetRoles =
    [
        RoleNames.Referent,
        RoleNames.CasaConvivenciaDirector,
        RoleNames.CasaConvivenciaCoordinator
    ];

    private readonly VicariaDbContext _dbContext;

    public MedicationReviewAlertService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CheckAndNotifyAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var cordobaNow = MedicationSchemeService.GetCordobaNow();
        var staleThreshold = cordobaNow.AddDays(-ThresholdDays);

        var activeSchemes = await _dbContext.MedicationSchemes
            .AsNoTracking()
            .Include(s => s.Person)
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        if (activeSchemes.Count == 0)
        {
            return 0;
        }

        var residentIds = (await _dbContext.SocialRecords
            .AsNoTracking()
            .Where(r => r.PersonType == PersonType.Resident)
            .Select(r => r.PersonId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var isFirstOfMonth = cordobaNow.Day == 1;
        var created = 0;

        foreach (var group in activeSchemes
                     .Where(s => s.Person != null && residentIds.Contains(s.PersonId))
                     .GroupBy(s => s.PersonId))
        {
            var person = group.First().Person!;
            var personName = $"{person.FirstName} {person.LastName}".Trim();
            var link = $"/dashboard/fichas/{group.Key}";

            if (group.Any(s => (s.ReviewedAt ?? s.LoadedAt) < staleThreshold))
            {
                created += await CreateForRolesAsync(
                    StaleEventType,
                    $"El esquema de medicación de {personName} lleva más de {ThresholdDays} días sin revisión.",
                    link,
                    nowUtc,
                    monthlyWindow: false,
                    cordobaNow,
                    cancellationToken);
            }

            if (isFirstOfMonth)
            {
                created += await CreateForRolesAsync(
                    MonthlyEventType,
                    $"Recordatorio mensual: revisar el esquema de medicación de {personName}.",
                    link,
                    nowUtc,
                    monthlyWindow: true,
                    cordobaNow,
                    cancellationToken);
            }
        }

        if (created > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    private async Task<int> CreateForRolesAsync(
        string eventType,
        string description,
        string link,
        DateTime nowUtc,
        bool monthlyWindow,
        DateTime cordobaNow,
        CancellationToken cancellationToken)
    {
        var windowStart = monthlyWindow
            ? new DateTime(cordobaNow.Year, cordobaNow.Month, 1)
            : nowUtc.AddDays(-ThresholdDays);

        var alreadyNotified = (await _dbContext.Notifications
            .Where(n => n.EventType == eventType && n.LinkUrl == link && n.CreatedAt >= windowStart)
            .Select(n => n.TargetRole!)
            .ToListAsync(cancellationToken)).ToHashSet();

        var created = 0;
        foreach (var role in TargetRoles)
        {
            if (alreadyNotified.Contains(role))
            {
                continue;
            }

            _dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Description = description,
                EventType = eventType,
                LinkUrl = link,
                IsRead = false,
                CreatedAt = nowUtc,
                TargetRole = role
            });
            created++;
        }

        return created;
    }
}
