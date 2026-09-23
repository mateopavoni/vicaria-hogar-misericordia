namespace Vicaria.Domain.Entities;

// motivos de egreso de la Casa de Convivencia (SCRUM-146): alta voluntaria del residente,
// alta decidida por el equipo, derivación a otra institución, abandono, u otro.
public enum StayExitReason
{
    VoluntaryDischarge,
    TeamDischarge,
    Referral,
    Abandonment,
    Other
}
