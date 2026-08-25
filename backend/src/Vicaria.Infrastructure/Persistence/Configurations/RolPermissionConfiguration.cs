using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class RolPermissionConfiguration : IEntityTypeConfiguration<RolPermission>
{
    public void Configure(EntityTypeBuilder<RolPermission> builder)
    {
        builder.ToTable("rol_permission");

        builder.HasKey(rp => new { rp.RolId, rp.PermissionId });

        // permisos de Directora de Casa de Convivencia (SCRUM-88)
        builder.HasData(
            new RolPermission { RolId = RolConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.VerFichasResidentesCasaConvivenciaId },
            new RolPermission { RolId = RolConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.CargarObservacionesResidentesId },
            new RolPermission { RolId = RolConfiguration.DirectoraDeCasonaId, PermissionId = PermissionConfiguration.VerAgendaMedicamentosId }
        );
    }
}
