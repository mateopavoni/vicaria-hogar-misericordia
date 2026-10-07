namespace Vicaria.Application.CasonaVisits;

public enum CreateCasonaVisitError
{
    PersonNotFound,
    PersonNotResident,
    NoOpenStay,
    TimeOverlap
}

public class CreateCasonaVisitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CreateCasonaVisitError? Error { get; init; }
    public Guid CasonaVisitId { get; init; }

    public static CreateCasonaVisitResult Ok(Guid casonaVisitId) => new()
    {
        Success = true,
        CasonaVisitId = casonaVisitId
    };

    public static CreateCasonaVisitResult PersonNotFound() =>
        new()
        {
            Success = false,
            Error = CreateCasonaVisitError.PersonNotFound,
            ErrorMessage = "La persona indicada no existe."
        };

    public static CreateCasonaVisitResult PersonNotResident() =>
        new()
        {
            Success = false,
            Error = CreateCasonaVisitError.PersonNotResident,
            ErrorMessage = "Solo se pueden agendar visitas para personas con tipo Residente."
        };

    public static CreateCasonaVisitResult NoOpenStay() =>
        new()
        {
            Success = false,
            Error = CreateCasonaVisitError.NoOpenStay,
            ErrorMessage = "La persona no tiene una estadía abierta en la Casa de Convivencia."
        };

    public static CreateCasonaVisitResult TimeOverlap() =>
        new()
        {
            Success = false,
            Error = CreateCasonaVisitError.TimeOverlap,
            ErrorMessage = "Ya hay una visita agendada que se superpone con ese horario."
        };
}
