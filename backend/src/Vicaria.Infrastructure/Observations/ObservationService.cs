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
}