using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Persons;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.SocialRecords;

public class SocialRecordService : ISocialRecordService
{
    private readonly VicariaDbContext _dbContext;

    public SocialRecordService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateSocialRecordResult> CreateAsync(CreateSocialRecordDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName?.Trim(),
            Dni = dto.Dni?.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Phone = dto.Phone?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.People.Add(person);

        var socialRecord = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            PersonType = dto.PersonType,
            ReasonForEntry = dto.ReasonForEntry?.Trim(),
            EntryDate = dto.EntryDate,
            HousingSituation = dto.HousingSituation?.Trim(),
            OvernightLocation = dto.OvernightLocation?.Trim(),
            Occupation = dto.Occupation?.Trim(),
            HasDocumentation = dto.HasDocumentation,
            GeneralNotes = dto.GeneralNotes?.Trim(),
            CreatedByUserId = actorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.SocialRecords.Add(socialRecord);

        if (dto.Contact is not null)
        {
            _dbContext.Contacts.Add(new Contact
            {
                Id = Guid.NewGuid(),
                SocialRecordId = socialRecord.Id,
                FirstName = dto.Contact.FirstName.Trim(),
                LastName = dto.Contact.LastName?.Trim(),
                Phone = dto.Contact.Phone?.Trim(),
                Address = dto.Contact.Address?.Trim()
            });
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Ficha social creada",
            AffectedEntity = $"SocialRecord:{socialRecord.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateSocialRecordResult.Ok(person.Id, socialRecord.Id);
    }

    public async Task<List<SocialRecordSearchResultDto>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        // filtra en memoria (no traduce a SQL), suficiente para el volumen de un centro barrial
        var records = await _dbContext.SocialRecords
            .Include(r => r.Person)
            .ToListAsync(cancellationToken);

        var normalizedQuery = Normalize(query);

        return records
            .Where(r => r.Person is not null && MatchesQuery(r.Person, normalizedQuery))
            .Select(r => new SocialRecordSearchResultDto(
                r.Id,
                r.PersonId,
                $"{r.Person!.FirstName} {r.Person.LastName}".Trim(),
                r.Person.Dni,
                r.UpdatedAt))
            .ToList();
    }

    public async Task<UpdateSocialRecordResult> UpdateAsync(Guid socialRecordId, UpdateSocialRecordDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var socialRecord = await _dbContext.SocialRecords
            .Include(r => r.Person)
            .FirstOrDefaultAsync(r => r.Id == socialRecordId, cancellationToken);

        if (socialRecord is null || socialRecord.Person is null)
        {
            return UpdateSocialRecordResult.NotFound();
        }

        socialRecord.Person.FirstName = dto.FirstName.Trim();
        socialRecord.Person.LastName = dto.LastName?.Trim();
        socialRecord.Person.Dni = dto.Dni?.Trim();
        socialRecord.Person.DateOfBirth = dto.DateOfBirth;
        socialRecord.Person.Phone = dto.Phone?.Trim();

        socialRecord.PersonType = dto.PersonType;
        socialRecord.ReasonForEntry = dto.ReasonForEntry?.Trim();
        socialRecord.EntryDate = dto.EntryDate;
        socialRecord.HousingSituation = dto.HousingSituation?.Trim();
        socialRecord.OvernightLocation = dto.OvernightLocation?.Trim();
        socialRecord.Occupation = dto.Occupation?.Trim();
        socialRecord.HasDocumentation = dto.HasDocumentation;
        socialRecord.GeneralNotes = dto.GeneralNotes?.Trim();
        socialRecord.UpdatedAt = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Ficha social editada",
            AffectedEntity = $"SocialRecord:{socialRecord.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateSocialRecordResult.Ok();
    }

    public async Task<UpdatePersonTypeResult> UpdatePersonTypeAsync(Guid personId, UpdatePersonTypeDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var person = await _dbContext.People.FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return UpdatePersonTypeResult.PersonNotFound();
        }

        var socialRecord = await _dbContext.SocialRecords
            .FirstOrDefaultAsync(r => r.PersonId == personId, cancellationToken);
        if (socialRecord is null)
        {
            return UpdatePersonTypeResult.SocialRecordNotFound();
        }

        // SCRUM-134: pasar a Residente exige una evaluación psiquiátrica vigente
        if (dto.PersonType == PersonType.Resident)
        {
            var hasValidEvaluation = await _dbContext.PsychiatricEvaluations
                .AnyAsync(e => e.PersonId == personId && e.IsValid, cancellationToken);

            if (!hasValidEvaluation)
            {
                return UpdatePersonTypeResult.MissingPsychiatricEvaluation();
            }
        }

        // SCRUM-141: al pasar a Residente se registra automáticamente una estadía en
        // la casona con EntryDate = hoy (solo en la transición, no al re-setear el tipo)
        var wasAlreadyResident = socialRecord.PersonType == PersonType.Resident;
        socialRecord.PersonType = dto.PersonType;
        socialRecord.UpdatedAt = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Tipo de persona actualizado",
            AffectedEntity = $"Person:{personId}",
            Date = DateTime.UtcNow
        });

        if (dto.PersonType == PersonType.Resident && !wasAlreadyResident)
        {
            var casonaStay = new CasonaStay
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                EntryDate = DateTime.UtcNow
            };
            _dbContext.CasonaStays.Add(casonaStay);

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorId,
                Action = "Estadía en casona registrada",
                AffectedEntity = $"CasonaStay:{casonaStay.Id}",
                Date = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdatePersonTypeResult.Ok();
    }
        public async Task<UpdatePersonProfileStatusResult> UpdatePersonProfileStatusAsync(Guid personId,UpdatePersonProfileStatusDto dto,Guid actorId,CancellationToken cancellationToken = default)
    {
        var person = await _dbContext.People.FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return UpdatePersonProfileStatusResult.PersonNotFound();
        }

        var socialRecord = await _dbContext.SocialRecords
            .FirstOrDefaultAsync(r => r.PersonId == personId, cancellationToken);
        if (socialRecord is null)
        {
            return UpdatePersonProfileStatusResult.SocialRecordNotFound();
        }

        if (dto.Status == PersonProfileStatus.Resident)
        {
            var hasValidEvaluation = await _dbContext.PsychiatricEvaluations
                .AnyAsync(e => e.PersonId == personId && e.IsValid, cancellationToken);

            if (!hasValidEvaluation)
            {
                return UpdatePersonProfileStatusResult.MissingPsychiatricEvaluation();
            }

            socialRecord.PersonType = PersonType.Resident;
            socialRecord.Status = SocialRecordStatus.Active;
        }
        else if (dto.Status == PersonProfileStatus.ActiveAmbulatory)
        {
            socialRecord.PersonType = PersonType.Ambulatory;
            socialRecord.Status = SocialRecordStatus.Active;
        }
        else if (dto.Status == PersonProfileStatus.InactiveAmbulatory)
        {
            socialRecord.PersonType = PersonType.Ambulatory;
            socialRecord.Status = SocialRecordStatus.Inactive;
        }

        socialRecord.UpdatedAt = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Estado de persona actualizado desde perfil",
            AffectedEntity = $"Person:{personId}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdatePersonProfileStatusResult.Ok();
    }

    private static bool MatchesQuery(Person person, string normalizedQuery)
    {
        return Normalize(person.FirstName).Contains(normalizedQuery)
            || Normalize(person.LastName ?? "").Contains(normalizedQuery)
            || Normalize(person.Dni ?? "").Contains(normalizedQuery);
    }

    // saca tildes y pasa a minúsculas para que la búsqueda las ignore
    private static string Normalize(string value)
    {
        var withoutAccents = value.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(withoutAccents.ToArray()).ToLowerInvariant();
    }
}
