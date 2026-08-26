using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    // ids fijos para que el seed sea determinístico entre entornos
    private static readonly Guid ReferenteId = new("11111111-1111-1111-1111-111111111111");
    // internal (no private): RolePermissionConfiguration necesita este id para sembrar los permisos de Directora
    internal static readonly Guid DirectoraDeCasonaId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid EscuchaId = new("33333333-3333-3333-3333-333333333333");
    // internal (no private): RolePermissionConfiguration necesita este id para sembrar los permisos del Coordinador (SCRUM-102)
    internal static readonly Guid CoordinadorDeCasaConvivenciaId = new("77777777-7777-7777-7777-777777777777");

    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("rol");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nombre)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Nombre)
            .IsUnique();

        builder.HasData(
            new Rol { Id = ReferenteId, Nombre = RolNombres.Referente },
            new Rol { Id = DirectoraDeCasonaId, Nombre = RolNombres.DirectoraDeCasona },
            new Rol { Id = EscuchaId, Nombre = RolNombres.Escucha },
            new Rol { Id = CoordinadorDeCasaConvivenciaId, Nombre = RolNombres.CoordinadorDeCasaConvivencia }
        );
    }
}
