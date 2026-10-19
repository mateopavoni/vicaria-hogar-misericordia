using Microsoft.EntityFrameworkCore;
using Vicaria.Application.MedicationStocks;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.MedicationStocks;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Medications;

public class MedicationStockServiceTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task RegisterStockEntryAsync_WhenMedicationDoesNotExist_CreatesCatalogEntryMovementAndAuditLog()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationStockService(context);

        var actorId = Guid.NewGuid();
        var expiration = DateTime.UtcNow.Date.AddMonths(6);
        var dto = new RegisterMedicationStockEntryDto("Paracetamol 500mg", 30, expiration, "Droguería del Sud");

        var result = await service.RegisterStockEntryAsync(dto, actorId);

        Assert.True(result.Success);
        Assert.NotNull(result.MedicationId);

        var catalog = await context.MedicationCatalogs.FindAsync(result.MedicationId.Value);
        Assert.NotNull(catalog);
        Assert.Equal("Paracetamol 500mg", catalog.Name);
        Assert.Equal(30, catalog.Stock);

        var movement = await context.MedicationStockMovements
            .FirstOrDefaultAsync(m => m.MedicationId == result.MedicationId.Value);
        Assert.NotNull(movement);
        Assert.Equal(MedicationStockMovementType.Ingreso, movement.Type);
        Assert.Equal(30, movement.Quantity);
        Assert.Equal(expiration, movement.ExpirationDate);
        Assert.Equal("Droguería del Sud", movement.Supplier);
        Assert.Equal(actorId, movement.CreatedByUserId);

        var auditLog = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "Ingreso de stock de medicamento registrado");
        Assert.NotNull(auditLog);
        Assert.Equal(actorId, auditLog.UserId);
        Assert.Equal($"MedicationCatalog:{catalog.Id}", auditLog.AffectedEntity);
    }

    [Fact]
    public async Task RegisterStockEntryAsync_WhenMedicationExists_IncrementsStockAndRecordsMovement()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationStockService(context);

        var actorId = Guid.NewGuid();
        var existing = new MedicationCatalog
        {
            Id = Guid.NewGuid(),
            Name = "Ibuprofeno 400mg",
            Stock = 10
        };
        context.MedicationCatalogs.Add(existing);
        await context.SaveChangesAsync();

        var expiration = DateTime.UtcNow.Date.AddMonths(12);
        var dto = new RegisterMedicationStockEntryDto("ibuprofeno 400mg", 20, expiration, "Farmacia Central");

        var result = await service.RegisterStockEntryAsync(dto, actorId);

        Assert.True(result.Success);
        Assert.Equal(existing.Id, result.MedicationId);

        var updated = await context.MedicationCatalogs.FindAsync(existing.Id);
        Assert.NotNull(updated);
        Assert.Equal(30, updated.Stock);

        var movement = await context.MedicationStockMovements
            .FirstOrDefaultAsync(m => m.MedicationId == existing.Id);
        Assert.NotNull(movement);
        Assert.Equal(20, movement.Quantity);
        Assert.Equal("Farmacia Central", movement.Supplier);
    }

    [Fact]
    public async Task GetMovementsByMedicationAsync_WhenMedicationExists_ReturnsOrderedHistory()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationStockService(context);

        var actorId = Guid.NewGuid();
        var medication = new MedicationCatalog
        {
            Id = Guid.NewGuid(),
            Name = "Clonazepam 2mg",
            Stock = 50
        };
        context.MedicationCatalogs.Add(medication);

        var movement1 = new MedicationStockMovement
        {
            Id = Guid.NewGuid(),
            MedicationId = medication.Id,
            Type = MedicationStockMovementType.Ingreso,
            Quantity = 40,
            Date = DateTime.UtcNow.AddDays(-2),
            Supplier = "Proveedor A",
            CreatedByUserId = actorId
        };
        var movement2 = new MedicationStockMovement
        {
            Id = Guid.NewGuid(),
            MedicationId = medication.Id,
            Type = MedicationStockMovementType.Ingreso,
            Quantity = 10,
            Date = DateTime.UtcNow.AddDays(-1),
            Supplier = "Proveedor B",
            CreatedByUserId = actorId
        };
        context.MedicationStockMovements.AddRange(movement1, movement2);
        await context.SaveChangesAsync();

        var result = await service.GetMovementsByMedicationAsync(medication.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(medication.Name, result.Data.MedicationName);
        Assert.Equal(50, result.Data.CurrentStock);
        Assert.Equal(2, result.Data.Movements.Count);
        Assert.Equal(10, result.Data.Movements[0].Quantity);
        Assert.Equal(40, result.Data.Movements[1].Quantity);
    }

    [Fact]
    public async Task GetMovementsByMedicationAsync_WhenMedicationDoesNotExist_ReturnsNotFound()
    {
        using var context = CreateInMemoryDbContext();
        var service = new MedicationStockService(context);

        var result = await service.GetMovementsByMedicationAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(GetMedicationStockHistoryError.MedicationNotFound, result.Error);
    }
}