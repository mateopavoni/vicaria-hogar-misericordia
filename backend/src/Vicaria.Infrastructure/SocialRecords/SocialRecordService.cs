using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Common;
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

    public async Task<List<SocialRecordSearchResultDto>> SearchAsync(string? query, PersonType? personTypeFilter = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var pattern = $"%{query.Trim().ToUpper()}%";

        if (_dbContext.Database.IsRelational())
        {
            return await _dbContext.SocialRecords
                .AsNoTracking()
                .Include(r => r.Person)
                .Where(r => r.Person != null
                    && (!personTypeFilter.HasValue || r.PersonType == personTypeFilter.Value)
                    && (
                        EF.Functions.Like(r.Person.FirstName.ToUpper(), pattern) ||
                        (r.Person.LastName != null && EF.Functions.Like(r.Person.LastName.ToUpper(), pattern)) ||
                        (r.Person.Dni != null && EF.Functions.Like(r.Person.Dni.ToUpper(), pattern)) ||
                        (r.Person.DateOfBirth != null && EF.Functions.Like(r.Person.DateOfBirth.ToString()!, pattern))
                    ))
                .Select(r => new SocialRecordSearchResultDto(
                    r.Id,
                    r.PersonId,
                    $"{r.Person!.FirstName} {r.Person.LastName}".Trim(),
                    r.Person.Dni,
                    r.UpdatedAt))
                .ToListAsync(cancellationToken);
        }

        // fallback en memoria (ej. InMemory DB de tests, que no soporta EF.Functions.Like):
        // normaliza tildes/mayúsculas a mano en vez de contar con el collation del motor real
        var records = await _dbContext.SocialRecords
            .AsNoTracking()
            .Include(r => r.Person)
            .ToListAsync(cancellationToken);

        var normalizedQuery = Normalize(query);

        var filtered = records
            .Where(r => r.Person is not null && MatchesQuery(r.Person, normalizedQuery));

        if (personTypeFilter.HasValue)
        {
            filtered = filtered.Where(r => r.PersonType == personTypeFilter.Value);
        }

        return filtered.Select(r => new SocialRecordSearchResultDto(r.Id, r.PersonId, $"{r.Person!.FirstName} {r.Person.LastName}".Trim(), r.Person.Dni, r.UpdatedAt)).ToList();
    }

    // perfil completo de una ficha, para la pantalla de detalle (SCRUM-8/121)
    public async Task<SocialRecordDetailDto?> GetByIdAsync(Guid socialRecordId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.SocialRecords
            .AsNoTracking()
            .Include(r => r.Person)
            .FirstOrDefaultAsync(r => r.Id == socialRecordId, cancellationToken);

        if (record is null || record.Person is null)
        {
            return null;
        }

        var contact = await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.SocialRecordId == socialRecordId, cancellationToken);

        var now = DateTime.UtcNow;
        var stays = await _dbContext.CasonaStays
            .AsNoTracking()
            .Where(s => s.PersonId == record.PersonId)
            .OrderByDescending(s => s.EntryDate)
            .Select(s => new SocialRecordStayDto(
                s.Id,
                s.EntryDate,
                s.ExitDate,
                s.ExitReason,
                Math.Max(0, (int)((s.ExitDate ?? now) - s.EntryDate).TotalDays)))
            .ToListAsync(cancellationToken);

        return new SocialRecordDetailDto(
            record.Id,
            record.PersonId,
            record.Person.FirstName,
            record.Person.LastName,
            record.Person.Dni,
            record.Person.DateOfBirth,
            record.Person.Phone,
            record.PersonType,
            record.ReasonForEntry,
            record.EntryDate,
            record.HousingSituation,
            record.OvernightLocation,
            record.Occupation,
            record.GeneralNotes,
            record.HasDocumentation,
            contact is null ? null : new ContactDto(contact.FirstName, contact.LastName, contact.Phone, contact.Address),
            record.Status,
            record.UpdatedAt,
            stays);
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
        var query = ApplyFilters(_dbContext.SocialRecords.Include(r => r.Person), filter);
        return await query.CountAsync(cancellationToken);
    }

    // listado paginado con busqueda de texto y filtros combinables (SCRUM-21/127)
    public async Task<PagedResult<SocialRecordListItemDto>> GetPagedAsync(int page, string? search, FilterSocialRecordsDto? filter, PersonType? personTypeFilter = null, CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;

        var query = _dbContext.SocialRecords.Include(r => r.Person).AsQueryable();

        if (filter is not null)
        {
            query = ApplyFilters(query, filter);
        }

        if (personTypeFilter.HasValue)
        {
            query = query.Where(r => r.PersonType == personTypeFilter.Value);
        }

        if (_dbContext.Database.IsRelational())
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim().ToUpper()}%";
                query = query.Where(r => r.Person != null && (
                    EF.Functions.Like(r.Person.FirstName.ToUpper(), pattern) ||
                    (r.Person.LastName != null && EF.Functions.Like(r.Person.LastName.ToUpper(), pattern)) ||
                    (r.Person.Dni != null && EF.Functions.Like(r.Person.Dni.ToUpper(), pattern))
                ));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(r => r.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => ToListItemDto(r))
                .ToListAsync(cancellationToken);

            return new PagedResult<SocialRecordListItemDto>(items, total, (int)Math.Ceiling(total / (double)pageSize));
        }

        // fallback en memoria (ej. InMemory DB de tests, que no soporta EF.Functions.Like)
        var allFiltered = await query.ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = Normalize(search);
            allFiltered = allFiltered
                .Where(r => r.Person is not null && MatchesQuery(r.Person, normalizedSearch))
                .ToList();
        }

        var totalMem = allFiltered.Count;
        var itemsMem = allFiltered
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => ToListItemDto(r))
            .ToList();

        return new PagedResult<SocialRecordListItemDto>(itemsMem, totalMem, (int)Math.Ceiling(totalMem / (double)pageSize));
    }

    private static SocialRecordListItemDto ToListItemDto(SocialRecord r) => new(
        r.Id,
        r.PersonId,
        r.Person!.FirstName,
        r.Person.LastName,
        r.Person.Dni,
        r.Person.DateOfBirth,
        r.PersonType,
        r.Status,
        r.UpdatedAt);

    private IQueryable<SocialRecord> ApplyFilters(IQueryable<SocialRecord> query, FilterSocialRecordsDto filter)
    {
        if (filter.Status.HasValue)
        {
            query = query.Where(r => r.Status == filter.Status.Value);
        }

        if (filter.PersonType.HasValue)
        {
            query = query.Where(r => r.PersonType == filter.PersonType.Value);
        }

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

        return query;
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
