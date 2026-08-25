using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    // ids fijos para que el seed sea determinístico entre entornos
    public static readonly Guid VerFichasResidentesCasaConvivenciaId = new("44444444-4444-4444-4444-444444444444");
    public static readonly Guid CargarObservacionesResidentesId = new("55555555-5555-5555-5555-555555555555");
    public static readonly Guid VerAgendaMedicamentosId = new("66666666-6666-6666-6666-666666666666");

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permission");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Codigo)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => p.Codigo)
            .IsUnique();

        builder.HasData(
            new Permission { Id = VerFichasResidentesCasaConvivenciaId, Codigo = PermissionNombres.VerFichasResidentesCasaConvivencia },
            new Permission { Id = CargarObservacionesResidentesId, Codigo = PermissionNombres.CargarObservacionesResidentes },
            new Permission { Id = VerAgendaMedicamentosId, Codigo = PermissionNombres.VerAgendaMedicamentos }
        );
    }
}
