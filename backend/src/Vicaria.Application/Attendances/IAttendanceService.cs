using Vicaria.Domain.Entities;

namespace Vicaria.Application.Attendances;

public interface IAttendanceService
{
    Task<RegisterAttendanceResult> RegisterAsync(CreateAttendanceDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<GetPersonAttendanceHistoryResult> GetPersonAttendanceHistoryAsync(Guid personId,GetPersonAttendanceHistoryFilterDto filter,Guid actorId,IEnumerable<string> actorRoles,CancellationToken cancellationToken = default);
    Task<DailyAttendanceDto> GetDailyAsync(AttendanceActivity activity, DateTime date, CancellationToken cancellationToken = default);
    Task<UpdateAttendanceResult> UpdateAsync(Guid attendanceId, UpdateAttendanceDto dto, Guid actorId, CancellationToken cancellationToken = default);
}