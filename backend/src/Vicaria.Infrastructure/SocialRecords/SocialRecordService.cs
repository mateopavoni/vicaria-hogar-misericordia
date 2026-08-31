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
            CreatedAt = DateTime.UtcNow
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
}
