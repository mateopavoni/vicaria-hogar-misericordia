using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
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
    public async Task<int> CountByFilterAsync(FilterSocialRecordsDto filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SocialRecords
            .Include(r => r.Person)
            .AsQueryable();

        if (filter.EntryDateFrom.HasValue)
        {
            query = query.Where(r => r.EntryDate >= filter.EntryDateFrom.Value);
        }

        if (filter.EntryDateTo.HasValue)
        {
            query = query.Where(r => r.EntryDate <= filter.EntryDateTo.Value);
        }

        if (filter.DaysWithoutObservations.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddDays(-filter.DaysWithoutObservations.Value);
            query = query.Where(r => r.UpdatedAt <= cutoff);
        }

        if (filter.HasDni.HasValue)
        {
            if (filter.HasDni.Value)
            {
                query = query.Where(r => r.Person != null && r.Person.Dni != null && r.Person.Dni != "");
            }
            else
            {
                query = query.Where(r => r.Person == null || r.Person.Dni == null || r.Person.Dni == "");
            }
        }

        if (filter.HasAddress.HasValue)
        {
            var recordsWithContactAddress = _dbContext.Contacts
                .Where(c => c.Address != null && c.Address != "")
                .Select(c => c.SocialRecordId);

            if (filter.HasAddress.Value)
            {
                query = query.Where(r => 
                    (r.OvernightLocation != null && r.OvernightLocation != "") || 
                    recordsWithContactAddress.Contains(r.Id));
            }
            else
            {
                query = query.Where(r => 
                    (r.OvernightLocation == null || r.OvernightLocation == "") && 
                    !recordsWithContactAddress.Contains(r.Id));
            }
        }

        return await query.CountAsync(cancellationToken);
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
