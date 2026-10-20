using Vicaria.Domain.Entities;

namespace Vicaria.Application.CasonaVisits;

public record ChangeCasonaVisitStatusDto(
    VisitStatus Status,
    string? CancellationReason);