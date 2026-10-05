using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    // ids fijos para que el seed sea determinístico entre entornos
    private static readonly Guid ReferentId = new("11111111-1111-1111-1111-111111111111");
    // internal (no private): RolePermissionConfiguration necesita este id para sembrar los permisos de Directora
    internal static readonly Guid CasonaDirectorId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ListenerId = new("33333333-3333-3333-3333-333333333333");
    // internal (no private): RolePermissionConfiguration necesita este id para sembrar los permisos del Coordinador (SCRUM-102)
    internal static readonly Guid CasaConvivenciaCoordinatorId = new("77777777-7777-7777-7777-777777777777");

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Name)
            .IsUnique();

        builder.HasData(
            new Role { Id = ReferentId, Name = RoleNames.Referent },
            new Role { Id = CasonaDirectorId, Name = RoleNames.CasonaDirector },
            new Role { Id = ListenerId, Name = RoleNames.Listener },
            new Role { Id = CasaConvivenciaCoordinatorId, Name = RoleNames.CasaConvivenciaCoordinator }
        );
    }
}
