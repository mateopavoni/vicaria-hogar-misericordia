using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CaritasReports;
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
                r.AttendanceReasonId,
                r.HasDocumentation
            })
            .ToListAsync(cancellationToken);

        var catalogIds = records
            .SelectMany(r => new[] { r.GenderId, r.HousingSituationId, r.OriginLocalityId, r.DocumentationStatusId, r.AttendanceReasonId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var catalogNames = await _dbContext.CaritasCatalogValues
            .AsNoTracking()
            .Where(v => catalogIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Name, cancellationToken);

        var male = 0;
        var female = 0;
        var other = 0;
        var street = 0;
        var housed = 0;
        var withDni = 0;
        var withoutDni = 0;
        var alcohol = 0;
        var drugs = 0;
        var violence = 0;
        var multiple = 0;
        var unspecified = 0;
        var origins = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            var gender = NameOf(record.GenderId);
            if (gender == "Masculino") male++;
            else if (gender == "Femenino") female++;
            else other++;

            var housing = NameOf(record.HousingSituationId);
            if (housing == "En situación de calle" || housing == "Transitorio") street++;
            else if (housing == "Con domicilio") housed++;

            var documentation = NameOf(record.DocumentationStatusId);
            if (documentation == "Con DNI") withDni++;
            else if (documentation == "Sin DNI" || documentation == "En trámite") withoutDni++;
            else if (record.HasDocumentation) withDni++;
            else withoutDni++;

            var origin = NameOf(record.OriginLocalityId) ?? "No informado";
            origins[origin] = origins.TryGetValue(origin, out var current) ? current + 1 : 1;

            switch (NameOf(record.AttendanceReasonId))
            {
                case "Consumo de alcohol": alcohol++; break;
                case "Consumo de drogas": drugs++; break;
                case "Violencia": violence++; break;
                case "Múltiple": multiple++; break;
                default: unspecified++; break;
            }
        }

        var report = new CaritasReportDto(
            type,
            fromDate.ToString("yyyy-MM-dd"),
            toDate.ToString("yyyy-MM-dd"),
            false,
            new CaritasReportTotalsDto(attendedPersons, average, newRegistrations),
            new CaritasReportGenderDto(male, female, other),
            new CaritasReportHousingDto(street, housed),
            new CaritasReportDniDto(withDni, withoutDni),
            origins
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new CaritasReportCountItemDto(kv.Key, kv.Value))
                .ToList(),
            new CaritasReportReasonsDto(alcohol, drugs, violence, multiple, unspecified));

        return CaritasReportResult.Ok(report);

        string? NameOf(Guid? id) =>
            id.HasValue && catalogNames.TryGetValue(id.Value, out var name) ? name : null;
    }

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
