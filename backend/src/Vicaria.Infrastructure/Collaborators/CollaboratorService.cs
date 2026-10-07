using System.Globalization;
using System.Text;
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

    // búsqueda por nombre, apellido o área, con filtro opcional por tipo (SCRUM-204).
    // mismo patrón que SocialRecordService.SearchAsync: en SQL Server la insensibilidad a
    // tildes/mayúsculas la dan la collation Modern_Spanish_CI_AI de las columnas + ToUpper(),
    // y en InMemory (tests unitarios) se normaliza a mano porque no existe ni collation ni
    // EF.Functions.Like. A diferencia de la búsqueda de personas, los comodines de LIKE
    // (%, _, [, ]) se escapan: la búsqueda es de texto literal.
    public async Task<List<CollaboratorSearchResultDto>> SearchAsync(
        string? query,
        CollaboratorType? typeFilter = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var trimmedQuery = query.Trim();

        if (_dbContext.Database.IsRelational())
        {
            var pattern = $"%{EscapeLikePattern(trimmedQuery.ToUpper())}%";

            var rows = await _dbContext.Collaborators
                .AsNoTracking()
                .Where(c => (!typeFilter.HasValue || c.Type == typeFilter.Value)
                    && (EF.Functions.Like(c.FirstName.ToUpper(), pattern, "\\")
                        || (c.LastName != null && EF.Functions.Like(c.LastName.ToUpper(), pattern, "\\"))
                        || (c.WorkArea != null && EF.Functions.Like(c.WorkArea.ToUpper(), pattern, "\\"))))
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Select(c => new { c.Id, c.FirstName, c.LastName, c.Phone, c.Email, c.Type, c.WorkArea })
                .ToListAsync(cancellationToken);

            return rows
                .Select(c => ToResultDto(c.Id, c.FirstName, c.LastName, c.Phone, c.Email, c.Type, c.WorkArea))
                .ToList();
        }

        // fallback en memoria (ej. InMemory DB de tests, que no soporta EF.Functions.Like):
        // normaliza tildes/mayúsculas a mano en vez de contar con el collation del motor real
        var normalizedQuery = Normalize(trimmedQuery);
        var collaborators = await _dbContext.Collaborators
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return collaborators
            .Where(c => (!typeFilter.HasValue || c.Type == typeFilter.Value)
                && MatchesQuery(c, normalizedQuery))
            .OrderBy(c => c.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.LastName, StringComparer.OrdinalIgnoreCase)
            .Select(c => ToResultDto(c.Id, c.FirstName, c.LastName, c.Phone, c.Email, c.Type, c.WorkArea))
            .ToList();
    }

    private static CollaboratorSearchResultDto ToResultDto(
        Guid id,
        string firstName,
        string? lastName,
        string? phone,
        string? email,
        CollaboratorType type,
        string? workArea) =>
        new(id, $"{firstName} {lastName}".Trim(), phone, email, type, workArea);

    private static bool MatchesQuery(Collaborator collaborator, string normalizedQuery)
    {
        return Normalize(collaborator.FirstName).Contains(normalizedQuery)
            || Normalize(collaborator.LastName ?? "").Contains(normalizedQuery)
            || Normalize(collaborator.WorkArea ?? "").Contains(normalizedQuery);
    }

    // escapa los comodines de LIKE para que "100%" o "a_b" busquen literalmente
    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("[", "\\[")
            .Replace("]", "\\]");

    // saca tildes y pasa a minúsculas para que la búsqueda las ignore
    // (mismo criterio que SocialRecordService.Normalize)
    private static string Normalize(string value)
    {
        var withoutAccents = value.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(withoutAccents.ToArray()).ToLowerInvariant();
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
