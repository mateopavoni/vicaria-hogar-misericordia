using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Attendances;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Attendances;

public class AttendanceStatisticsServiceTests
{
    private static VicariaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new VicariaDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static Attendance NewAttendance(DateTime date, int anonymousCount = 0) => new()
    {
        Id = Guid.NewGuid(),
        PersonId = Guid.NewGuid(),
        Date = date,
        Activity = AttendanceActivity.DayCenter,
        AnonymousCount = anonymousCount,
        CreatedByUserId = Guid.NewGuid()
    };

    [Fact]
    public async Task GetStatisticsAsync_ComparaConElPeriodoAnteriorContandoConYSinFicha()
    {
        using var db = CreateDbContext();
        var from = new DateTime(2026, 11, 10);
        var to = new DateTime(2026, 11, 11);
        // período actual: 2 con ficha + 4 sin ficha = 6
        db.Attendances.AddRange(NewAttendance(from, 4), NewAttendance(to));
        // período anterior (8 y 9/11): 3 con ficha + 1 sin ficha = 4
        db.Attendances.AddRange(NewAttendance(from.AddDays(-1), 1), NewAttendance(from.AddDays(-1)), NewAttendance(from.AddDays(-2)));
        await db.SaveChangesAsync();

        var result = await new AttendanceStatisticsService(db).GetStatisticsAsync(from, to, null);

        Assert.True(result.Success);
        Assert.Equal(4, result.Data!.PreviousPeriodTotal);
        Assert.Equal(50, result.Data.ComparisonPercentage);
    }

    [Fact]
    public async Task GetStatisticsAsync_SinDatosAnteriores_NoCalculaComparacion()
    {
        using var db = CreateDbContext();
        var day = new DateTime(2026, 11, 10);
        db.Attendances.Add(NewAttendance(day, 2));
        await db.SaveChangesAsync();

        var result = await new AttendanceStatisticsService(db).GetStatisticsAsync(day, day, null);

        Assert.Equal(0, result.Data!.PreviousPeriodTotal);
        Assert.Null(result.Data.ComparisonPercentage);
    }
}
