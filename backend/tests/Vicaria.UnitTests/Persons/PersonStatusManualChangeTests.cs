using Microsoft.EntityFrameworkCore;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.SocialRecords;

namespace Vicaria.UnitTests.Persons;

public class PersonStatusManualChangeTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenSwitchingToInactive_CreatesHistoryAuditAndNotificationForReferent()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);

        var actorId = Guid.NewGuid();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Mario", LastName = "Bros" };
        var socialRecord = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.People.Add(person);
        context.SocialRecords.Add(socialRecord);
        await context.SaveChangesAsync();

        var dto = new UpdatePersonStatusDto(SocialRecordStatus.Inactive, "Viaje familiar prolongado");
        var result = await service.UpdateStatusAsync(person.Id, dto, actorId);

        Assert.True(result.Success);

        var updatedRecord = await context.SocialRecords.FindAsync(socialRecord.Id);
        Assert.NotNull(updatedRecord);
        Assert.Equal(SocialRecordStatus.Inactive, updatedRecord.Status);

        var history = await context.PersonStatusChanges.FirstOrDefaultAsync(h => h.PersonId == person.Id);
        Assert.NotNull(history);
        Assert.Equal(SocialRecordStatus.Active, history.PreviousStatus);
        Assert.Equal(SocialRecordStatus.Inactive, history.NewStatus);
        Assert.Equal("Viaje familiar prolongado", history.Reason);
        Assert.Equal(actorId, history.ChangedByUserId);

        var notification = await context.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Equal(RoleNames.Referent, notification.TargetRole);
        Assert.Contains("Mario Bros", notification.Description);
        Assert.Contains("Viaje familiar prolongado", notification.Description);

        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action.Contains("Estado de persona cambiado manualmente"));
        Assert.NotNull(auditLog);
        Assert.Equal(actorId, auditLog.UserId);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenSwitchingToActive_DoesNotCreateNotification()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);

        var actorId = Guid.NewGuid();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Luigi", LastName = "Bros" };
        var socialRecord = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Inactive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.People.Add(person);
        context.SocialRecords.Add(socialRecord);
        await context.SaveChangesAsync();

        var dto = new UpdatePersonStatusDto(SocialRecordStatus.Active, "Retomó asistencia al centro");
        var result = await service.UpdateStatusAsync(person.Id, dto, actorId);

        Assert.True(result.Success);

        var updatedRecord = await context.SocialRecords.FindAsync(socialRecord.Id);
        Assert.NotNull(updatedRecord);
        Assert.Equal(SocialRecordStatus.Active, updatedRecord.Status);

        var notificationCount = await context.Notifications.CountAsync();
        Assert.Equal(0, notificationCount);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenSameStatus_ReturnsConflictSameStatus()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Toad" };
        var socialRecord = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.People.Add(person);
        context.SocialRecords.Add(socialRecord);
        await context.SaveChangesAsync();

        var dto = new UpdatePersonStatusDto(SocialRecordStatus.Active, "Mismo estado");
        var result = await service.UpdateStatusAsync(person.Id, dto, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(UpdatePersonStatusError.SameStatus, result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_IncluyeHistorialDeEstadosConMotivoYSistemaEnLosAutomaticos()
    {
        using var context = CreateInMemoryDbContext();
        var service = new SocialRecordService(context);

        var actor = new User { Id = Guid.NewGuid(), FirstName = "Ana", LastName = "Referente", Email = "ana@test.com" };
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Mario", LastName = "Bros" };
        var socialRecord = new SocialRecord
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Status = SocialRecordStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(actor);
        context.People.Add(person);
        context.SocialRecords.Add(socialRecord);
        context.PersonStatusChanges.Add(new PersonStatusChange
        {
            PersonId = person.Id,
            PreviousStatus = SocialRecordStatus.Active,
            NewStatus = SocialRecordStatus.Inactive,
            Reason = "Paso automático a inactivo por 30 días sin asistencia",
            ChangedByUserId = null,
            ChangedAt = DateTime.UtcNow.AddDays(-2)
        });
        context.PersonStatusChanges.Add(new PersonStatusChange
        {
            PersonId = person.Id,
            PreviousStatus = SocialRecordStatus.Inactive,
            NewStatus = SocialRecordStatus.Active,
            Reason = "Volvió a asistir",
            ChangedByUserId = actor.Id,
            ChangedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var detail = await service.GetByIdAsync(socialRecord.Id);

        Assert.NotNull(detail);
        Assert.Equal(2, detail.StatusHistory.Count);
        var latest = detail.StatusHistory[0];
        Assert.Equal("Inactivo", latest.PreviousValue);
        Assert.Equal("Activo", latest.NewValue);
        Assert.Equal("Ana Referente", latest.ModifiedBy);
        Assert.Equal("Volvió a asistir", latest.Reason);
        Assert.Equal("Sistema", detail.StatusHistory[1].ModifiedBy);
    }
}
