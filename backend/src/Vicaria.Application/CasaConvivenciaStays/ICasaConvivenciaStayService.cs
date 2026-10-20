namespace Vicaria.Application.CasaConvivenciaStays;

public interface ICasaConvivenciaStayService
{
    Task<CasaConvivenciaStayExitResult> ExitAsync(Guid stayId, CasaConvivenciaStayExitDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CasaConvivenciaStayDto>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
