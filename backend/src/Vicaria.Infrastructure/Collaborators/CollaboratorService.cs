using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Collaborators;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Collaborators;

public class CollaboratorService : ICollaboratorService
{
    private readonly VicariaDbContext _dbContext;

    public CollaboratorService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateCollaboratorResult> CreateAsync(
        CreateCollaboratorDto dto,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var dni = NormalizeDni(dto.Dni);

        if (dni is not null && await _dbContext.Collaborators
                .AsNoTracking()
                .AnyAsync(c => c.Dni == dni, cancellationToken))
        {
            return CreateCollaboratorResult.DuplicateDni();
        }

        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName?.Trim(),
            Dni = dni,
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Type = dto.Type,
            WorkArea = dto.WorkArea?.Trim(),
            RegisteredByUserId = actorId,
            RegisteredAt = DateTime.UtcNow
        };
        _dbContext.Collaborators.Add(collaborator);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Colaborador creado",
            AffectedEntity = $"Collaborator:{collaborator.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateCollaboratorResult.Ok(collaborator.Id);
    }

    // deja solo los dígitos del DNI para que el índice único no distinga "38.123.456" de
    // "38123456" (mismo criterio que SocialRecordService.NormalizeDni)
    private static string? NormalizeDni(string? dni)
    {
        if (string.IsNullOrWhiteSpace(dni)) return null;
        var digitsOnly = new string(dni.Where(char.IsDigit).ToArray());
        return digitsOnly.Length > 0 ? digitsOnly : dni.Trim();
    }
}
