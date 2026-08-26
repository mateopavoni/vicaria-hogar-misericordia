using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence;

public class VicariaDbContext : DbContext
{
    public VicariaDbContext(DbContextOptions<VicariaDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VicariaDbContext).Assembly);
    }
}
