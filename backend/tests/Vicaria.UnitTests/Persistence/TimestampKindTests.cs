using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Persistence;

public class TimestampKindTests
{
    [Fact]
    public async Task Timestamps_AreReadAsUtc_AndDatesWithoutTimeAreNot()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var personId = Guid.NewGuid();

        await using (var db = new VicariaDbContext(options))
        {
            db.People.Add(new Person
            {
                Id = personId,
                FirstName = "Ana",
                CreatedAt = new DateTime(2026, 11, 4, 0, 51, 0, DateTimeKind.Unspecified),
                DateOfBirth = new DateTime(1980, 5, 10, 0, 0, 0, DateTimeKind.Unspecified)
            });
            await db.SaveChangesAsync();
        }

        await using (var db = new VicariaDbContext(options))
        {
            var person = await db.People.SingleAsync(p => p.Id == personId);

            Assert.Equal(DateTimeKind.Utc, person.CreatedAt.Kind);
            Assert.Equal(DateTimeKind.Unspecified, person.DateOfBirth!.Value.Kind);
        }
    }
}
