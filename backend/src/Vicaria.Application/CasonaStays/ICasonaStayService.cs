namespace Vicaria.Application.CasonaStays;

public interface ICasonaStayService
{
    Task<CasonaStayExitResult> ExitAsync(Guid stayId, CasonaStayExitDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CasonaStayDto>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
