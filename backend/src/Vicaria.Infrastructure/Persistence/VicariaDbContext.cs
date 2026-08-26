using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.Persistence;

public class VicariaDbContext : DbContext
{
    public VicariaDbContext(DbContextOptions<VicariaDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VicariaDbContext).Assembly);
    }
}
