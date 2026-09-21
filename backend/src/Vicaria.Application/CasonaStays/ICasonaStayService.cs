namespace Vicaria.Application.CasonaStays;

public interface ICasonaStayService
{
    Task<CasonaStayExitResult> ExitAsync(Guid stayId, CasonaStayExitDto dto, Guid actorId, CancellationToken cancellationToken = default);
}
