using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("rol_permission");

        builder.HasKey(rp => new { rp.RolId, rp.PermissionId });

        // permisos de Directora de Casa de Convivencia (SCRUM-88)
        builder.HasData(
            new RolePermission { RolId = RolConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.ViewCasaConvivenciaResidentRecordsId },
            new RolePermission { RolId = RolConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.LoadResidentObservationsId },
            new RolePermission { RolId = RolConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.ViewMedicationScheduleId }
        );
    }
}
