using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.PsychiatricEvaluations;

namespace Vicaria.Infrastructure.Attendances;

public class AttendanceInactivityService : IAttendanceInactivityService
{
    private readonly VicariaDbContext _dbContext;

    public AttendanceInactivityService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CheckAndProcessInactivityAsync(CancellationToken cancellationToken = default)
    {
        // SCRUM-135: una persona queda Activa solo si registró asistencia en los últimos
        // 30 días; sin asistencia reciente (o sin ninguna asistencia) pasa a Inactive.
        // SCRUM-262: el pase se registra en el historial de estados; los Residentes
        // con estadía abierta quedan exentos (siguen presentes físicamente aunque no
        // tengan un registro de asistencia explícito).
        // Los 30 días se cuentan desde lo último entre la última asistencia, el alta de la
        // ficha y el último pase a Activo: una ficha nueva o recién reactivada a mano no
        // vuelve a Inactiva hasta cumplir 30 días sin asistencia.
        var thresholdDate = DateTime.UtcNow.AddDays(-30);
        // las asistencias se guardan con la fecha de Córdoba: cuentan las de los últimos 30
        // días de Córdoba, hoy incluido
        var attendanceThreshold = PsychiatricEvaluationService.GetCordobaToday().AddDays(-29);

        var inactiveCandidates = await _dbContext.SocialRecords
            .Include(r => r.Person)
            .Where(r => r.Status == SocialRecordStatus.Active
                && r.CreatedAt < thresholdDate
                && !_dbContext.Attendances.Any(a => a.PersonId == r.PersonId && a.Date >= attendanceThreshold)
                && !_dbContext.PersonStatusChanges.Any(c => c.PersonId == r.PersonId
                    && c.NewStatus == SocialRecordStatus.Active
                    && c.ChangedAt >= thresholdDate)
                && !_dbContext.CasaConvivenciaStays.Any(s => s.PersonId == r.PersonId && s.ExitDate == null))
            .ToListAsync(cancellationToken);

        if (inactiveCandidates.Count == 0)
        {
            return;
        }

        foreach (var record in inactiveCandidates)
        {
            record.Status = SocialRecordStatus.Inactive;
            record.UpdatedAt = DateTime.UtcNow;

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = record.CreatedByUserId,
                Action = "Paso automático a inactivo por 30 días sin asistencia",
                AffectedEntity = $"SocialRecord:{record.Id}",
                Date = DateTime.UtcNow
            });

            _dbContext.PersonStatusChanges.Add(new PersonStatusChange
            {
                Id = Guid.NewGuid(),
                PersonId = record.PersonId,
                PreviousStatus = SocialRecordStatus.Active,
                NewStatus = SocialRecordStatus.Inactive,
                Reason = "Paso automático a inactivo por 30 días sin asistencia",
                ChangedByUserId = null,
                ChangedAt = DateTime.UtcNow
            });

            // SCRUM-67: los Referentes reciben la misma notificación que en el cambio manual
            var fullName = $"{record.Person?.FirstName} {record.Person?.LastName}".Trim();
            _dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Description = $"La persona {fullName} pasó a estado Inactivo. Motivo: 30 días sin asistencia",
                EventType = "PersonStatusInactive",
                LinkUrl = $"/personas/{record.PersonId}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                TargetRole = RoleNames.Referent
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}