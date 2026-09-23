using System.Text;
using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Observations;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Observations;

public class ObservationService : IObservationService
{
    private readonly VicariaDbContext _dbContext;

    public ObservationService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateObservationResult> CreateObservationAsync(
        Guid personId,
        CreateObservationDto dto,
        Guid authorUserId,
        CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            return new CreateObservationResult(
                Success: false,
                Error: CreateObservationError.PersonNotFound,
                ErrorMessage: "La persona especificada no existe.");
        }

        ObservationCategory? category = null;
        if (dto.CategoryId.HasValue)
        {
            category = await _dbContext.ObservationCategories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value, cancellationToken);

            if (category is null || !category.IsActive)
            {
                return new CreateObservationResult(
                    Success: false,
                    Error: CreateObservationError.CategoryNotFoundOrInactive,
                    ErrorMessage: "La categoría especificada no existe o no se encuentra activa.");
            }
        }

        var author = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == authorUserId, cancellationToken);

        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Content = dto.Content.Trim(),
            CategoryId = dto.CategoryId,
            AuthorUserId = authorUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Observations.Add(observation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var authorName = author is not null ? $"{author.FirstName} {author.LastName}".Trim() : string.Empty;

        var response = new ObservationResponseDto(
            Id: observation.Id,
            PersonId: observation.PersonId,
            Content: observation.Content,
            CategoryId: observation.CategoryId,
            CategoryName: category?.Name,
            AuthorUserId: observation.AuthorUserId,
            AuthorName: authorName,
            CreatedAt: observation.CreatedAt
        );

        return new CreateObservationResult(Success: true, Data: response);
    }
    public async Task<ObservationsTimelineResponseDto> GetTimelineAsync(
        Guid personId,
        GetObservationsFilterDto filters,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(personId, filters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => ToResponseDto(o))
            .ToListAsync(cancellationToken);

        return new ObservationsTimelineResponseDto(items, totalCount);
    }

    // exportacion de las observaciones filtradas a CSV, mismo filtro que el timeline (SCRUM-166)
    public async Task<string> ExportToCsvAsync(
        Guid personId,
        GetObservationsFilterDto filters,
        CancellationToken cancellationToken = default)
    {
        var items = await ApplyFilters(personId, filters)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => ToResponseDto(o))
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Fecha,Categoría,Autor,Contenido");
        foreach (var item in items)
        {
            csv.AppendLine(string.Join(',',
                item.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                CsvEscape(item.CategoryName ?? ""),
                CsvEscape(item.AuthorName),
                CsvEscape(item.Content)));
        }

        return csv.ToString();
    }

    private IQueryable<Observation> ApplyFilters(Guid personId, GetObservationsFilterDto filters)
    {
        // Include obligatorio: ToResponseDto lee o.Category/o.AuthorUser para armar
        // CategoryName/AuthorName; sin esto quedan siempre null/vacío (bug encontrado en QA local)
        var query = _dbContext.Observations
            .AsNoTracking()
            .Include(o => o.Category)
            .Include(o => o.AuthorUser)
            .Where(o => o.PersonId == personId);

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(o => o.CategoryId == filters.CategoryId.Value);
        }

        if (filters.AuthorUserId.HasValue)
        {
            query = query.Where(o => o.AuthorUserId == filters.AuthorUserId.Value);
        }

        if (filters.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filters.FromDate.Value);
        }

        if (filters.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filters.ToDate.Value);
        }

        return query;
    }

    private static ObservationResponseDto ToResponseDto(Observation o) => new(
        o.Id,
        o.PersonId,
        o.Content,
        o.CategoryId,
        o.Category != null ? o.Category.Name : null,
        o.AuthorUserId,
        o.AuthorUser != null ? (o.AuthorUser.FirstName + " " + o.AuthorUser.LastName).Trim() : string.Empty,
        o.CreatedAt);

    // envuelve en comillas y escapa comillas internas si el valor tiene coma, comilla o salto de linea
    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}