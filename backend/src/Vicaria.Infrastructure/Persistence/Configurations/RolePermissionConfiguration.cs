using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Directora y Coordinador de Casa de Convivencia tienen los mismos 3 permisos:
        // ver fichas, cargar observaciones y ver la agenda de medicación
        builder.HasData(
            new RolePermission { RoleId = RoleConfiguration.CasaConvivenciaDirectorId, PermissionId = PermissionConfiguration.ViewCasaConvivenciaResidentRecordsId },
            new RolePermission { RoleId = RoleConfiguration.CasaConvivenciaDirectorId, PermissionId = PermissionConfiguration.LoadResidentObservationsId },
            new RolePermission { RoleId = RoleConfiguration.CasaConvivenciaDirectorId, PermissionId = PermissionConfiguration.ViewMedicationScheduleId },
            new RolePermission { RoleId = RoleConfiguration.CasaConvivenciaCoordinatorId, PermissionId = PermissionConfiguration.ViewCasaConvivenciaResidentRecordsId },
            new RolePermission { RoleId = RoleConfiguration.CasaConvivenciaCoordinatorId, PermissionId = PermissionConfiguration.LoadResidentObservationsId },
            new RolePermission { RoleId = RoleConfiguration.CasaConvivenciaCoordinatorId, PermissionId = PermissionConfiguration.ViewMedicationScheduleId }
        );
    }
}
