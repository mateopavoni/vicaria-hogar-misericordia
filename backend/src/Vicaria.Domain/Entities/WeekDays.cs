namespace Vicaria.Domain.Entities;

// días de la semana para eventos recurrentes (SCRUM-183), guardado como int:
// las plantillas del hogar usan Monday..Friday (L-V) o solo Tuesday (merendero)
[Flags]
public enum WeekDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64
}
