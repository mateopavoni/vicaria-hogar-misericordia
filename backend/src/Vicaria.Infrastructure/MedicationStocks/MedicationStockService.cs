using Microsoft.EntityFrameworkCore;
using Vicaria.Application.MedicationStocks;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.MedicationStocks;

public class MedicationStockService : IMedicationStockService
{
    private readonly VicariaDbContext _dbContext;

    public MedicationStockService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegisterMedicationStockResult> RegisterStockEntryAsync(RegisterMedicationStockEntryDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var trimmedName = dto.Nombre.Trim();

        var medication = await _dbContext.MedicationCatalogs
            .FirstOrDefaultAsync(m => m.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (medication is null)
        {
            medication = new MedicationCatalog
            {
                Id = Guid.NewGuid(),
                Name = trimmedName,
                Stock = dto.Cantidad
            };
            _dbContext.MedicationCatalogs.Add(medication);
        }
        else
        {
            medication.Stock += dto.Cantidad;
        }

        var movement = new MedicationStockMovement
        {
            Id = Guid.NewGuid(),
            MedicationId = medication.Id,
            Type = MedicationStockMovementType.Ingreso,
            Quantity = dto.Cantidad,
            Date = DateTime.UtcNow,
            ExpirationDate = dto.FechaVencimiento.Date,
            Supplier = string.IsNullOrWhiteSpace(dto.Proveedor) ? null : dto.Proveedor.Trim(),
            CreatedByUserId = actorId
        };

        _dbContext.MedicationStockMovements.Add(movement);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Ingreso de stock de medicamento registrado",
            AffectedEntity = $"MedicationCatalog:{medication.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RegisterMedicationStockResult.Ok(medication.Id);
    }

    public async Task<GetMedicationStockHistoryResult> GetMovementsByMedicationAsync(Guid medicationId, CancellationToken cancellationToken = default)
    {
        var medication = await _dbContext.MedicationCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == medicationId, cancellationToken);

        if (medication is null)
        {
            return GetMedicationStockHistoryResult.NotFound();
        }

        var movements = await _dbContext.MedicationStockMovements
            .AsNoTracking()
            .Where(m => m.MedicationId == medicationId)
            .OrderByDescending(m => m.Date)
            .Select(m => new MedicationStockMovementItemDto(
                m.Id,
                m.MedicationId,
                medication.Name,
                m.Type,
                m.Type == MedicationStockMovementType.Ingreso ? "Ingreso" : "Consumo",
                m.Quantity,
                m.Date,
                m.ExpirationDate,
                m.Supplier,
                m.CreatedByUserId
            ))
            .ToListAsync(cancellationToken);

        var dto = new MedicationStockHistoryDto(
            medication.Id,
            medication.Name,
            medication.Stock,
            movements
        );

        return GetMedicationStockHistoryResult.Ok(dto);
    }

    public async Task<IReadOnlyList<MedicationStockAlertDto>> GetStockAlertsAsync(CancellationToken cancellationToken = default)
    {
        var lowStockMedications = await _dbContext.MedicationCatalogs
            .AsNoTracking()
            .Where(m => m.Stock <= m.MinimumStockThreshold)
            .OrderBy(m => m.Stock)
            .ThenBy(m => m.Name)
            .Select(m => new MedicationStockAlertDto(
                m.Id,
                m.Name,
                m.Stock,
                m.MinimumStockThreshold,
                m.Stock == 0
            ))
            .ToListAsync(cancellationToken);

        return lowStockMedications;
    }

    public async Task<UpdateMedicationStockThresholdResult> UpdateThresholdAsync(Guid medicationId, UpdateMedicationStockThresholdDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var medication = await _dbContext.MedicationCatalogs
            .FirstOrDefaultAsync(m => m.Id == medicationId, cancellationToken);

        if (medication is null)
        {
            return UpdateMedicationStockThresholdResult.NotFound();
        }

        medication.MinimumStockThreshold = dto.MinimumStockThreshold;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"Umbral mínimo de stock actualizado a {dto.MinimumStockThreshold}",
            AffectedEntity = $"MedicationCatalog:{medication.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateMedicationStockThresholdResult.Ok();
    }
}