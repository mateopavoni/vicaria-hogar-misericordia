namespace Vicaria.Application.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int TotalPages);
