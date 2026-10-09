using Vicaria.Domain.Entities;

namespace Vicaria.Application.CasonaVisits;

// edición completa de una visita (SCRUM-210): solo aplica mientras esté Pending;
// CancellationReason es opcional también al cancelar.
public record UpdateCasonaVisitDto(
    Guid PersonId,
    string VisitorName,
    DateTime Date,
    TimeSpan StartTime,
    int EstimatedDurationMinutes,
    VisitStatus Status,
    string? CancellationReason,
    bool AllowOverlap = false);
