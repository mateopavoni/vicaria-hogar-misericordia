using Microsoft.EntityFrameworkCore;
using Vicaria.Application.PersonExports;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.PersonExports;

// arma el documento de exportación de la ficha (SCRUM-252) y registra la
// auditoría; el PDF lo genera IPersonExportPdfGenerator
public class PersonExportService : IPersonExportService
{
    private static readonly Dictionary<string, string> SectionLabels = new()
    {
        [PersonExportSections.PersonalData] = "solo datos personales",
        [PersonExportSections.DataWithObservations] = "datos y observaciones",
        [PersonExportSections.All] = "datos, historia de vida y observaciones"
    };

    private readonly VicariaDbContext _dbContext;

    public PersonExportService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PersonExportResult> GetExportAsync(Guid personId, string sections, Guid actorId, CancellationToken cancellationToken = default)
    {
        var normalizedSections = (sections ?? string.Empty).Trim().ToLowerInvariant();
        if (!PersonExportSections.Allowed.Contains(normalizedSections))
        {
            return PersonExportResult.InvalidSections();
        }

        var person = await _dbContext.People
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return PersonExportResult.PersonNotFound();
        }

        var socialRecord = await _dbContext.SocialRecords
            .FirstOrDefaultAsync(r => r.PersonId == personId, cancellationToken);
        if (socialRecord is null)
        {
            return PersonExportResult.SocialRecordNotFound();
        }

        var actor = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
        var generatedByName = actor is null
            ? "Sistema"
            : $"{actor.FirstName} {actor.LastName}".Trim();
        var generatedByRole = actor?.Role?.Name ?? string.Empty;

        IReadOnlyList<PersonExportObservationItem> observations = [];
        if (normalizedSections is PersonExportSections.DataWithObservations or PersonExportSections.All)
        {
            // mismo orden que el timeline (más recientes primero), sin autor
            // (decisión: excluir datos de terceros/otros usuarios)
            observations = await _dbContext.Observations
                .Where(o => o.PersonId == personId)
                .Include(o => o.Category)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new PersonExportObservationItem(
                    o.CreatedAt,
                    o.Category != null ? o.Category.Name : null,
                    o.Content))
                .ToListAsync(cancellationToken);
        }

        string? beforeCentroBarrial = null;
        string? inCentroBarrial = null;
        string? afterCentroBarrial = null;
        if (normalizedSections == PersonExportSections.All)
        {
            var lifeStory = await _dbContext.LifeStories
                .FirstOrDefaultAsync(l => l.PersonId == personId, cancellationToken);
            beforeCentroBarrial = lifeStory?.BeforeCentroBarrial;
            inCentroBarrial = lifeStory?.InCentroBarrial;
            afterCentroBarrial = lifeStory?.AfterCentroBarrial;
        }

        var generatedAt = DateTime.UtcNow;
        var document = new PersonExportDocument(
            PersonId: person.Id,
            FirstName: person.FirstName,
            LastName: person.LastName,
            Dni: person.Dni,
            DateOfBirth: person.DateOfBirth,
            Phone: person.Phone,
            PersonType: socialRecord.PersonType switch
            {
                Domain.Entities.PersonType.Ambulatory => "Ambulatorio",
                Domain.Entities.PersonType.Resident => "Residente",
                _ => null
            },
            ReasonForEntry: socialRecord.ReasonForEntry,
            EntryDate: socialRecord.EntryDate,
            HousingSituation: socialRecord.HousingSituation,
            OvernightLocation: socialRecord.OvernightLocation,
            Occupation: socialRecord.Occupation,
            HasDocumentation: socialRecord.HasDocumentation,
            GeneralNotes: socialRecord.GeneralNotes,
            Sections: normalizedSections,
            Observations: observations,
            BeforeCentroBarrial: beforeCentroBarrial,
            InCentroBarrial: inCentroBarrial,
            AfterCentroBarrial: afterCentroBarrial,
            GeneratedByName: generatedByName,
            GeneratedByRole: generatedByRole,
            GeneratedAtUtc: generatedAt);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"Ficha exportada (PDF): {SectionLabels[normalizedSections]}",
            AffectedEntity = $"Person:{personId}",
            Date = generatedAt
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return PersonExportResult.Ok(document);
    }
}
