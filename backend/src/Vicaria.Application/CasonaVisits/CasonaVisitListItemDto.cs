using Vicaria.Domain.Entities;

namespace Vicaria.Application.CasonaVisits;

// ítem del listado de visitas por rango (SCRUM-210), pensado para el calendario semanal
public record CasonaVisitListItemDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string VisitorName,
    DateTime Date,
    TimeSpan StartTime,
    int EstimatedDurationMinutes,
    VisitStatus Status,
    string? CancellationReason);
