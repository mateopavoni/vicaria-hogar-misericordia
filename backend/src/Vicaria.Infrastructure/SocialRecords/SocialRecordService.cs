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
            Dni = NormalizeDni(dto.Dni),
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
        var dniDigits = DigitsOnly(query);
        var dniPattern = dniDigits.Length > 0 ? $"%{dniDigits}%" : null;

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
                        (dniPattern != null && r.Person.Dni != null && EF.Functions.Like(r.Person.Dni, dniPattern)) ||
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
            .Where(r => r.Person is not null && MatchesQuery(r.Person, normalizedQuery, dniDigits));

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
        var stays = await _dbContext.CasaConvivenciaStays
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

        // historial de cambios de tipo de persona (bug reportado 2026-09-23: nunca existió
        // este campo). Se materializa primero y se mapea en memoria (en vez de armar el
        // string dentro del Select) para no asumir que ChangedByUser siempre resuelve.
        var personTypeChanges = await _dbContext.PersonTypeChanges
            .AsNoTracking()
            .Include(c => c.ChangedByUser)
            .Where(c => c.PersonId == record.PersonId)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        var personTypeHistory = personTypeChanges
            .Select(c => new PersonTypeHistoryItemDto(
                c.Id,
                c.PreviousType.HasValue ? PersonTypeLabel(c.PreviousType) : null,
                PersonTypeLabel(c.NewType),
                c.ChangedByUser is null ? "Usuario desconocido" : $"{c.ChangedByUser.FirstName} {c.ChangedByUser.LastName}".Trim(),
                c.ChangedAt))
            .ToList();

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
            stays,
            personTypeHistory);
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

        // el tipo de persona puede afectar la estadía en la Casa de Convivencia y queda
        // auditado en el historial; se valida/aplica antes de tocar el resto de los campos
        // (bug reportado 2026-09-23: este endpoint cambiaba PersonType sin la misma lógica
        // que UpdatePersonTypeAsync, permitiendo desincronizar el estado de la estadía)
        if (dto.PersonType.HasValue)
        {
            var outcome = await ChangePersonTypeAsync(socialRecord, dto.PersonType.Value, actorId, cancellationToken);
            switch (outcome)
            {
                case PersonTypeChangeOutcome.MissingPsychiatricEvaluation:
                    return UpdateSocialRecordResult.MissingPsychiatricEvaluation();
                case PersonTypeChangeOutcome.ActiveStayMustBeExitedFirst:
                    return UpdateSocialRecordResult.ActiveStayMustBeExitedFirst();
            }
        }

        socialRecord.Person.FirstName = dto.FirstName.Trim();
        socialRecord.Person.LastName = dto.LastName?.Trim();
        socialRecord.Person.Dni = NormalizeDni(dto.Dni);
        socialRecord.Person.DateOfBirth = dto.DateOfBirth;
        socialRecord.Person.Phone = dto.Phone?.Trim();

        socialRecord.ReasonForEntry = dto.ReasonForEntry?.Trim();
        socialRecord.EntryDate = dto.EntryDate;
        socialRecord.HousingSituation = dto.HousingSituation?.Trim();
        socialRecord.OvernightLocation = dto.OvernightLocation?.Trim();
        socialRecord.Occupation = dto.Occupation?.Trim();
        socialRecord.HasDocumentation = dto.HasDocumentation;
        socialRecord.GeneralNotes = dto.GeneralNotes?.Trim();
        socialRecord.UpdatedAt = DateTime.UtcNow;

        // bug reportado 2026-09-23: UpdateSocialRecordDto no tenía Contact, así que
        // editar el contacto de referencia se perdía en silencio
        await UpsertContactAsync(socialRecordId, dto.Contact, cancellationToken);

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
                var dniDigits = DigitsOnly(search);
                var dniPattern = dniDigits.Length > 0 ? $"%{dniDigits}%" : null;

                query = query.Where(r => r.Person != null && (
                    EF.Functions.Like(r.Person.FirstName.ToUpper(), pattern) ||
                    (r.Person.LastName != null && EF.Functions.Like(r.Person.LastName.ToUpper(), pattern)) ||
                    (dniPattern != null && r.Person.Dni != null && EF.Functions.Like(r.Person.Dni, dniPattern))
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
            var dniDigits = DigitsOnly(search);
            allFiltered = allFiltered
                .Where(r => r.Person is not null && MatchesQuery(r.Person, normalizedSearch, dniDigits))
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

        var outcome = await ChangePersonTypeAsync(socialRecord, dto.PersonType, actorId, cancellationToken);
        switch (outcome)
        {
            case PersonTypeChangeOutcome.MissingPsychiatricEvaluation:
                return UpdatePersonTypeResult.MissingPsychiatricEvaluation();
            case PersonTypeChangeOutcome.ActiveStayMustBeExitedFirst:
                return UpdatePersonTypeResult.ActiveStayMustBeExitedFirst();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdatePersonTypeResult.Ok();
    }

    public async Task<UpdatePersonProfileStatusResult> UpdatePersonProfileStatusAsync(Guid personId, UpdatePersonProfileStatusDto dto, Guid actorId, CancellationToken cancellationToken = default)
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

        var targetType = dto.Status == PersonProfileStatus.Resident ? PersonType.Resident : PersonType.Ambulatory;
        var outcome = await ChangePersonTypeAsync(socialRecord, targetType, actorId, cancellationToken);
        switch (outcome)
        {
            case PersonTypeChangeOutcome.MissingPsychiatricEvaluation:
                return UpdatePersonProfileStatusResult.MissingPsychiatricEvaluation();
            case PersonTypeChangeOutcome.ActiveStayMustBeExitedFirst:
                return UpdatePersonProfileStatusResult.ActiveStayMustBeExitedFirst();
        }

        socialRecord.Status = dto.Status == PersonProfileStatus.InactiveAmbulatory
            ? SocialRecordStatus.Inactive
            : SocialRecordStatus.Active;
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

    private enum PersonTypeChangeOutcome
    {
        Ok,
        NoChange,
        MissingPsychiatricEvaluation,
        ActiveStayMustBeExitedFirst
    }

    // lógica única de cambio de PersonType, compartida por los 3 endpoints que pueden
    // tocarlo (UpdatePersonTypeAsync, UpdateAsync de ficha, UpdatePersonProfileStatusAsync).
    // Bug reportado 2026-09-23: antes solo UpdatePersonTypeAsync validaba la evaluación
    // psiquiátrica y manejaba la estadía; los otros dos caminos podían desincronizar el
    // tipo de persona con la estadía real de la Casa de Convivencia. No hace SaveChanges:
    // el caller decide cuándo persistir junto con el resto de sus cambios.
    private async Task<PersonTypeChangeOutcome> ChangePersonTypeAsync(SocialRecord socialRecord, PersonType newType, Guid actorId, CancellationToken cancellationToken)
    {
        if (socialRecord.PersonType == newType)
        {
            return PersonTypeChangeOutcome.NoChange;
        }

        var wasResident = socialRecord.PersonType == PersonType.Resident;

        if (newType == PersonType.Resident)
        {
            // SCRUM-134: pasar a Residente exige una evaluación psiquiátrica vigente
            var hasValidEvaluation = await _dbContext.PsychiatricEvaluations
                .AnyAsync(e => e.PersonId == socialRecord.PersonId && e.IsValid, cancellationToken);

            if (!hasValidEvaluation)
            {
                return PersonTypeChangeOutcome.MissingPsychiatricEvaluation;
            }
        }
        else if (wasResident)
        {
            // no se puede dejar de ser Residente con una estadía todavía abierta: el
            // egreso se registra por el flujo dedicado (CasaConvivenciaStayController)
            var hasActiveStay = await _dbContext.CasaConvivenciaStays
                .AnyAsync(s => s.PersonId == socialRecord.PersonId && s.ExitDate == null, cancellationToken);

            if (hasActiveStay)
            {
                return PersonTypeChangeOutcome.ActiveStayMustBeExitedFirst;
            }
        }

        var previousType = socialRecord.PersonType;
        socialRecord.PersonType = newType;
        socialRecord.UpdatedAt = DateTime.UtcNow;

        _dbContext.PersonTypeChanges.Add(new PersonTypeChange
        {
            Id = Guid.NewGuid(),
            PersonId = socialRecord.PersonId,
            PreviousType = previousType,
            NewType = newType,
            ChangedByUserId = actorId,
            ChangedAt = DateTime.UtcNow
        });

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Tipo de persona actualizado",
            AffectedEntity = $"Person:{socialRecord.PersonId}",
            Date = DateTime.UtcNow
        });

        // SCRUM-141: al pasar a Residente se registra automáticamente una estadía en
        // la casa de convivencia con EntryDate = hoy (solo en la transición, no al re-setear el tipo)
        if (newType == PersonType.Resident && !wasResident)
        {
            var casaConvivenciaStay = new CasaConvivenciaStay
            {
                Id = Guid.NewGuid(),
                PersonId = socialRecord.PersonId,
                EntryDate = DateTime.UtcNow
            };
            _dbContext.CasaConvivenciaStays.Add(casaConvivenciaStay);

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorId,
                Action = "Estadía en casa de convivencia registrada",
                AffectedEntity = $"CasaConvivenciaStay:{casaConvivenciaStay.Id}",
                Date = DateTime.UtcNow
            });
        }

        return PersonTypeChangeOutcome.Ok;
    }

    private async Task UpsertContactAsync(Guid socialRecordId, ContactDto? contactDto, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Contacts
            .FirstOrDefaultAsync(c => c.SocialRecordId == socialRecordId, cancellationToken);

        if (contactDto is null)
        {
            if (existing is not null)
            {
                _dbContext.Contacts.Remove(existing);
            }
            return;
        }

        if (existing is null)
        {
            _dbContext.Contacts.Add(new Contact
            {
                Id = Guid.NewGuid(),
                SocialRecordId = socialRecordId,
                FirstName = contactDto.FirstName.Trim(),
                LastName = contactDto.LastName?.Trim(),
                Phone = contactDto.Phone?.Trim(),
                Address = contactDto.Address?.Trim()
            });
            return;
        }

        existing.FirstName = contactDto.FirstName.Trim();
        existing.LastName = contactDto.LastName?.Trim();
        existing.Phone = contactDto.Phone?.Trim();
        existing.Address = contactDto.Address?.Trim();
    }

    private static string PersonTypeLabel(PersonType? type) => type switch
    {
        PersonType.Ambulatory => "Ambulatorio",
        PersonType.Resident => "Residente",
        _ => "Sin definir"
    };

    private static bool MatchesQuery(Person person, string normalizedQuery, string dniDigitsQuery)
    {
        return Normalize(person.FirstName).Contains(normalizedQuery)
            || Normalize(person.LastName ?? "").Contains(normalizedQuery)
            || (dniDigitsQuery.Length > 0 && (person.Dni ?? "").Contains(dniDigitsQuery));
    }

    // saca tildes y pasa a minúsculas para que la búsqueda las ignore
    private static string Normalize(string value)
    {
        var withoutAccents = value.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(withoutAccents.ToArray()).ToLowerInvariant();
    }

    // deja solo los dígitos del DNI (bug reportado 2026-09-23: "38.123.456" guardado y
    // "38123456" buscado no matcheaban porque la comparación era literal)
    private static string DigitsOnly(string value) => new(value.Where(char.IsDigit).ToArray());

    private static string? NormalizeDni(string? dni)
    {
        if (string.IsNullOrWhiteSpace(dni)) return null;
        var digitsOnly = DigitsOnly(dni);
        return digitsOnly.Length > 0 ? digitsOnly : dni.Trim();
    }
}
