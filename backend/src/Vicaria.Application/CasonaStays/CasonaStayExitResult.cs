namespace Vicaria.Application.CasonaStays;

public enum CasonaStayExitError
{
    StayNotFound,
    AlreadyExited
}

public class CasonaStayExitResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CasonaStayExitError? Error { get; init; }

    public static CasonaStayExitResult Ok() => new() { Success = true };

    public static CasonaStayExitResult StayNotFound() =>
        new() { Success = false, Error = CasonaStayExitError.StayNotFound, ErrorMessage = "La estadía no existe." };

    public static CasonaStayExitResult AlreadyExited() =>
        new() { Success = false, Error = CasonaStayExitError.AlreadyExited, ErrorMessage = "La estadía ya fue egresada." };
}
