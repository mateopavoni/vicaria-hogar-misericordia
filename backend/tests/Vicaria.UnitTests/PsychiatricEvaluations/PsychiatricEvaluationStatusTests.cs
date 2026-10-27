using Microsoft.EntityFrameworkCore;
using Vicaria.Application.PsychiatricEvaluations;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;
using Vicaria.Infrastructure.PsychiatricEvaluations;

namespace Vicaria.UnitTests.PsychiatricEvaluations;

public class PsychiatricEvaluationStatusTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task GetStatusAsync_WhenNoEvaluation_ReturnsHasEvaluationFalseAndEstaVencidaTrue()
    {
        using var context = CreateInMemoryDbContext();
        var service = new PsychiatricEvaluationService(context);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Juan" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var result = await service.GetStatusAsync(person.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.False(result.Data.HasEvaluation);
        Assert.True(result.Data.EstaVencida);
        Assert.False(result.Data.AlertaVencimientoProximo);
        Assert.Null(result.Data.DiasParaVencer);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEvaluationExpiresIn15Days_CalculatesDiasParaVencerAndAlertaTrue()
    {
        using var context = CreateInMemoryDbContext();
        var service = new PsychiatricEvaluationService(context);

        var today = PsychiatricEvaluationService.GetCordobaToday();
        var targetExpiration = today.AddDays(15);
        var evaluationDate = targetExpiration.AddMonths(-6);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Marcos" };
        var evaluation = new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Date = evaluationDate,
            Professional = "Dra. Gómez",
            Diagnosis = "F10.2",
            IsValid = true,
            RegisteredByUserId = Guid.NewGuid()
        };

        context.People.Add(person);
        context.PsychiatricEvaluations.Add(evaluation);
        await context.SaveChangesAsync();

        var result = await service.GetStatusAsync(person.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.HasEvaluation);
        Assert.False(result.Data.EstaVencida);
        Assert.True(result.Data.AlertaVencimientoProximo);
        Assert.Equal(15, result.Data.DiasParaVencer);
    }

    [Fact]
    public async Task GetStatusAsync_WhenEvaluationOlderThan6Months_ReturnsEstaVencidaTrue()
    {
        using var context = CreateInMemoryDbContext();
        var service = new PsychiatricEvaluationService(context);

        var today = PsychiatricEvaluationService.GetCordobaToday();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Pedro" };
        var evaluation = new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Date = today.AddMonths(-6).AddDays(-1),
            IsValid = true,
            RegisteredByUserId = Guid.NewGuid()
        };

        context.People.Add(person);
        context.PsychiatricEvaluations.Add(evaluation);
        await context.SaveChangesAsync();

        var result = await service.GetStatusAsync(person.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.EstaVencida);
        Assert.False(result.Data.AlertaVencimientoProximo);
        Assert.True(result.Data.DiasParaVencer < 0);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsOnlyInvalidEvaluationsOrdered()
    {
        using var context = CreateInMemoryDbContext();
        var service = new PsychiatricEvaluationService(context);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Lucas" };
        var actor = new User { Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User" };

        var current = new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Date = DateTime.UtcNow.Date,
            IsValid = true,
            RegisteredByUserId = actor.Id
        };

        var old1 = new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Date = DateTime.UtcNow.Date.AddMonths(-8),
            IsValid = false,
            RegisteredByUserId = actor.Id
        };

        var old2 = new PsychiatricEvaluation
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Date = DateTime.UtcNow.Date.AddMonths(-14),
            IsValid = false,
            RegisteredByUserId = actor.Id
        };

        context.People.Add(person);
        context.Users.Add(actor);
        context.PsychiatricEvaluations.AddRange(current, old1, old2);
        await context.SaveChangesAsync();

        var result = await service.GetHistoryAsync(person.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.History);
        Assert.Equal(2, result.History.Count);
        Assert.Equal(old1.Id, result.History[0].Id);
        Assert.Equal(old2.Id, result.History[1].Id);
    }
}