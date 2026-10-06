using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Timelines;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Timelines;

// proyección del timeline unificado del perfil (SCRUM-159): combina los hitos
// ya registrados en el expediente (ingresos/egresos de Casa de Convivencia) con las
// observaciones, ordenados por fecha descendente para la vista del perfil
public class ProfileTimelineService : IProfileTimelineService
{
    private readonly VicariaDbContext _dbContext;

    public ProfileTimelineService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProfileTimelineResult> GetTimelineAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            return new ProfileTimelineResult(
                Success: false,
                Error: ProfileTimelineError.PersonNotFound,
                ErrorMessage: "La persona especificada no existe.");
        }

        var entries = new List<ProfileTimelineEntryDto>();

        var observations = await _dbContext.Observations
            .AsNoTracking()
            .Where(o => o.PersonId == personId)
            .Select(o => new ProfileTimelineEntryDto(
                o.Id,
                ProfileTimelineEntryType.Observation,
                o.CreatedAt,
                "Observación",
                o.Content,
                o.CategoryId,
                o.Category != null ? o.Category.Name : null,
                o.AuthorUser != null ? (o.AuthorUser.FirstName + " " + o.AuthorUser.LastName).Trim() : string.Empty
            ))
            .ToListAsync(cancellationToken);

        entries.AddRange(observations);

        var stays = await _dbContext.CasaConvivenciaStays
            .AsNoTracking()
            .Where(s => s.PersonId == personId)
            .ToListAsync(cancellationToken);

        foreach (var stay in stays)
        {
            entries.Add(new ProfileTimelineEntryDto(
                stay.Id,
                ProfileTimelineEntryType.CasaConvivenciaStayEntry,
                stay.EntryDate,
                "Ingreso a la Casa de Convivencia",
                null,
                null,
                null,
                null));

            if (stay.ExitDate.HasValue)
            {
                entries.Add(new ProfileTimelineEntryDto(
                    stay.Id,
                    ProfileTimelineEntryType.CasaConvivenciaStayExit,
                    stay.ExitDate.Value,
                    "Egreso de la Casa de Convivencia",
                    null,
                    null,
                    null,
                    null));
            }
        }

        var ordered = entries.OrderByDescending(e => e.Date).ToList();

        return new ProfileTimelineResult(
            Success: true,
            Data: new ProfileTimelineResponseDto(ordered, ordered.Count));
    }
}