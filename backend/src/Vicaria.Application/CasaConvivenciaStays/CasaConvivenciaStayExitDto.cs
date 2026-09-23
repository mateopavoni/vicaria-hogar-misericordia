using Vicaria.Domain.Entities;

namespace Vicaria.Application.CasaConvivenciaStays;

// DTO para el egreso de una estadía en la Casa de Convivencia (SCRUM-146).
// ExitReason y Reason son opcionales: si se provee ExitReason=Other, Reason es texto libre.
public record CasaConvivenciaStayExitDto(
    StayExitReason? ExitReason,
    string? Reason,
    SocialRecordStatus? NewStatus = SocialRecordStatus.Active
);
