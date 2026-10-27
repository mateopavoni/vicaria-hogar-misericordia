namespace Vicaria.Application.CaritasReports;

// informe estadístico para Caritas (SCRUM-270): mismo contrato que consume la
// pantalla de SCRUM-271, con números reales (IsSample = false)
public record CaritasReportTotalsDto(
    int AttendedPersons,
    double AverageAttendancesPerPerson,
    int NewRegistrations);

public record CaritasReportGenderDto(int Male, int Female, int Other);

public record CaritasReportHousingDto(int Street, int Housed);

public record CaritasReportDniDto(int WithDni, int WithoutDni);

public record CaritasReportCountItemDto(string Label, int Count);

public record CaritasReportReasonsDto(
    int Alcohol,
    int Drugs,
    int Violence,
    int Multiple,
    int Unspecified);

public record CaritasReportDto(
    string PeriodType,
    string From,
    string To,
    bool IsSample,
    CaritasReportTotalsDto Totals,
    CaritasReportGenderDto Gender,
    CaritasReportHousingDto Housing,
    CaritasReportDniDto Dni,
    IReadOnlyList<CaritasReportCountItemDto> Geography,
    CaritasReportReasonsDto Reasons);

public record GetCaritasReportQueryDto(string Type, DateTime To);

public enum CaritasReportError { InvalidPeriod, InvalidDate }

public class CaritasReportResult
{
    public bool Success { get; init; }
    public CaritasReportDto? Data { get; init; }
    public CaritasReportError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static CaritasReportResult Ok(CaritasReportDto data) =>
        new() { Success = true, Data = data };

    public static CaritasReportResult InvalidPeriod(string message = "El período debe ser trimestral o semestral.") =>
        new() { Success = false, Error = CaritasReportError.InvalidPeriod, ErrorMessage = message };

    public static CaritasReportResult InvalidDate(string message = "La fecha de corte no es válida.") =>
        new() { Success = false, Error = CaritasReportError.InvalidDate, ErrorMessage = message };
}
