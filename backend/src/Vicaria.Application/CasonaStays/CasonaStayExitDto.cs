using Vicaria.Domain.Entities;

namespace Vicaria.Application.CasonaStays;

// DTO para el egreso de una estadía en la Casona (SCRUM-146).
// ExitReason y Reason son opcionales: si se provee ExitReason=Other, Reason es texto libre.
public record CasonaStayExitDto(StayExitReason? ExitReason, string? Reason);
