namespace Vicaria.Application.CasonaVisits;

public enum UpdateCasonaVisitError
{
    NotFound,
    InvalidState,
    VisitInPast,
    PersonNotFound,
    PersonNotResident,
    NoOpenStay,
    TimeOverlap
}

public class UpdateCasonaVisitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UpdateCasonaVisitError? Error { get; init; }

    public static UpdateCasonaVisitResult Ok() => new() { Success = true };

    public static UpdateCasonaVisitResult NotFound() =>
        new()
        {
            Success = false,
            Error = UpdateCasonaVisitError.NotFound,
            ErrorMessage = "La visita no existe."
        };

    public static UpdateCasonaVisitResult InvalidState() =>
        new()
        {
            Success = false,
            Error = UpdateCasonaVisitError.InvalidState,
            ErrorMessage = "La visita no puede modificarse porque ya está realizada o cancelada."
        };

    public static UpdateCasonaVisitResult VisitInPast() =>
        new()
        {
            Success = false,
            Error = UpdateCasonaVisitError.VisitInPast,
            ErrorMessage = "La fecha y hora de la visita no puede ser en el pasado."
        };

    public static UpdateCasonaVisitResult PersonNotFound() =>
        new()
        {
            Success = false,
            Error = UpdateCasonaVisitError.PersonNotFound,
            ErrorMessage = "La persona indicada no existe."
        };

    public static UpdateCasonaVisitResult PersonNotResident() =>
        new()
        {
            Success = false,
            Error = UpdateCasonaVisitError.PersonNotResident,
            ErrorMessage = "Solo se pueden agendar visitas para personas con tipo Residente."
        };

    public static UpdateCasonaVisitResult NoOpenStay() =>
        new()
        {
            Success = false,
            Error = UpdateCasonaVisitError.NoOpenStay,
            ErrorMessage = "La persona no tiene una estadía abierta en la Casa de Convivencia."
        };

    public static UpdateCasonaVisitResult TimeOverlap() =>
        new()
        {
            Success = false,
            Error = UpdateCasonaVisitError.TimeOverlap,
            ErrorMessage = "Ya hay una visita agendada que se superpone con ese horario."
        };
}
