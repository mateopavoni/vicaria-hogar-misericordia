using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("rol_permission");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // permisos de Directora de Casa de Convivencia (SCRUM-88)
        // permisos de Coordinador de Casa de Convivencia (SCRUM-102): mismos 3 permisos que Directora,
        // según los criterios de aceptación de SCRUM-73 (ver fichas, cargar observaciones, ver agenda)
        builder.HasData(
            new RolePermission { RoleId = RoleConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.ViewCasaConvivenciaResidentRecordsId },
            new RolePermission { RoleId = RoleConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.LoadResidentObservationsId },
            new RolePermission { RoleId = RoleConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.ViewMedicationScheduleId },
            new RolePermission { RoleId = RoleConfiguration.CoordinadorDeCasaConvivenciaId, PermissionId = PermissionConfiguration.ViewCasaConvivenciaResidentRecordsId },
            new RolePermission { RoleId = RoleConfiguration.CoordinadorDeCasaConvivenciaId, PermissionId = PermissionConfiguration.LoadResidentObservationsId },
            new RolePermission { RoleId = RoleConfiguration.CoordinadorDeCasaConvivenciaId, PermissionId = PermissionConfiguration.ViewMedicationScheduleId }
        );
    }
}
