namespace Vicaria.Application.CasaConvivenciaStays;

public enum CasaConvivenciaStayExitError
{
    StayNotFound,
    AlreadyExited
}

public class CasaConvivenciaStayExitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CasaConvivenciaStayExitError? Error { get; init; }

    public static CasaConvivenciaStayExitResult Ok() => new() { Success = true };

    public static CasaConvivenciaStayExitResult StayNotFound() =>
        new() { Success = false, Error = CasaConvivenciaStayExitError.StayNotFound, ErrorMessage = "La estadía no existe." };

    public static CasaConvivenciaStayExitResult AlreadyExited() =>
        new() { Success = false, Error = CasaConvivenciaStayExitError.AlreadyExited, ErrorMessage = "La estadía ya fue egresada." };
}
