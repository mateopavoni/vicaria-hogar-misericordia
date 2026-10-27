using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CaritasReports;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.CaritasReports;

// desglose por motivo, totales y base del PDF del informe Caritas (SCRUM-270):
// personas atendidas = asistencias nominales distintas en el rango;
// el rango replica la regla del front (periodRange): últimos 3/6 meses
// que terminan en la fecha de corte, inclusive
public class CaritasReportService : ICaritasReportService
{
    private readonly VicariaDbContext _dbContext;

    public CaritasReportService(VicariaDbContext dbContext) => _dbContext = dbContext;

    public async Task<CaritasReportResult> GetReportAsync(string type, DateTime to, CancellationToken cancellationToken = default)
    {
        if (type != "trimestral" && type != "semestral") return CaritasReportResult.InvalidPeriod();
        var toDate = to.Date;
        if (toDate > DateTime.UtcNow.Date) return CaritasReportResult.InvalidDate();

        var fromDate = ResolveFrom(type, toDate);
        var toExclusive = toDate.AddDays(1);

        var attendancePersonIds = await _dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.Date >= fromDate && a.Date < toExclusive)
            .Select(a => a.PersonId)
            .ToListAsync(cancellationToken);

        var totalRecords = attendancePersonIds.Count;
        var attendedIds = attendancePersonIds.ToHashSet();
        var attendedPersons = attendedIds.Count;
        var average = attendedPersons == 0
            ? 0
            : Math.Round(totalRecords / (double)attendedPersons, 1);

        var newRegistrations = await _dbContext.SocialRecords
            .AsNoTracking()
            .CountAsync(r => r.CreatedAt >= fromDate && r.CreatedAt < toExclusive, cancellationToken);

        var records = await _dbContext.SocialRecords
            .AsNoTracking()
            .Where(r => attendedIds.Contains(r.PersonId))
            .Select(r => new
            {
                r.GenderId,
                r.HousingSituationId,
                r.OriginLocalityId,
                r.DocumentationStatusId,
                r.AttendanceReasonId
            })
            .ToListAsync(cancellationToken);

        // todos los valores del catálogo: los activos aparecen aunque cuenten 0 y los dados
        // de baja solo si alguna ficha todavía los usa (baja lógica, SCRUM-274)
        var catalogValues = await _dbContext.CaritasCatalogValues
            .AsNoTracking()
            .OrderBy(v => v.CreatedAt)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

        var gender = Breakdown(CaritasCatalogCategory.Gender, records.Select(r => r.GenderId));
        var housing = Breakdown(CaritasCatalogCategory.HousingSituation, records.Select(r => r.HousingSituationId));
        var dni = Breakdown(CaritasCatalogCategory.DocumentationStatus, records.Select(r => r.DocumentationStatusId));
        var reasons = Breakdown(CaritasCatalogCategory.AttendanceReason, records.Select(r => r.AttendanceReasonId));
        // la geografía puede tener muchas localidades: solo las que tienen personas, de mayor a menor
        var geography = Breakdown(CaritasCatalogCategory.OriginLocality, records.Select(r => r.OriginLocalityId))
            .Where(i => i.Count > 0)
            .OrderByDescending(i => i.Count)
            .ThenBy(i => i.Label, StringComparer.Ordinal)
            .ToList();

        var report = new CaritasReportDto(
            type,
            fromDate.ToString("yyyy-MM-dd"),
            toDate.ToString("yyyy-MM-dd"),
            false,
            new CaritasReportTotalsDto(attendedPersons, average, newRegistrations),
            gender,
            housing,
            dni,
            geography,
            reasons);

        return CaritasReportResult.Ok(report);

        // cuenta por id del catálogo (no por nombre, así un valor renombrado se sigue
        // contando); lo que no tiene valor, o tiene uno de otra categoría, va a "No informado"
        List<CaritasReportCountItemDto> Breakdown(CaritasCatalogCategory category, IEnumerable<Guid?> ids)
        {
            var values = catalogValues.Where(v => v.Category == category).ToList();
            var counts = values.ToDictionary(v => v.Id, _ => 0);
            var notInformed = 0;

            foreach (var id in ids)
            {
                if (id.HasValue && counts.TryGetValue(id.Value, out var current)) counts[id.Value] = current + 1;
                else notInformed++;
            }

            var items = values
                .Where(v => v.IsActive || counts[v.Id] > 0)
                .Select(v => new CaritasReportCountItemDto(v.Name, counts[v.Id]))
                .ToList();
            if (notInformed > 0) items.Add(new CaritasReportCountItemDto(NotInformedLabel, notInformed));
            return items;
        }
    }

    private const string NotInformedLabel = "No informado";

    private static DateTime ResolveFrom(string type, DateTime toDate)
    {
        var months = type == "trimestral" ? 3 : 6;
        if (toDate.AddDays(1).Day == 1)
        {
            return new DateTime(toDate.Year, toDate.Month, 1).AddMonths(-(months - 1));
        }

        var firstOfTargetMonth = new DateTime(toDate.Year, toDate.Month, 1).AddMonths(-months);
        var lastDay = DateTime.DaysInMonth(firstOfTargetMonth.Year, firstOfTargetMonth.Month);
        return firstOfTargetMonth.AddDays(Math.Min(toDate.Day, lastDay));
    }
}
