using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    public DbSet<Person> People => Set<Person>();
    public DbSet<SocialRecord> SocialRecords => Set<SocialRecord>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<CasaConvivenciaStay> CasaConvivenciaStays => Set<CasaConvivenciaStay>();
    public DbSet<PsychiatricEvaluation> PsychiatricEvaluations => Set<PsychiatricEvaluation>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<ObservationCategory> ObservationCategories => Set<ObservationCategory>();
    public DbSet<CaritasCatalogValue> CaritasCatalogValues => Set<CaritasCatalogValue>();
    public DbSet<LifeStory> LifeStories => Set<LifeStory>();
    public DbSet<LifeStoryEntry> LifeStoryEntries => Set<LifeStoryEntry>();
    public DbSet<PersonTypeChange> PersonTypeChanges => Set<PersonTypeChange>();
    public DbSet<PersonStatusChange> PersonStatusChanges => Set<PersonStatusChange>();
    public DbSet<GeneralCalendarEvent> GeneralCalendarEvents => Set<GeneralCalendarEvent>();
    public DbSet<PersonalCalendarEvent> PersonalCalendarEvents => Set<PersonalCalendarEvent>();
    public DbSet<CasonaVisit> CasonaVisits => Set<CasonaVisit>();
    public DbSet<Collaborator> Collaborators => Set<Collaborator>();
    public DbSet<MedicationCatalog> MedicationCatalogs => Set<MedicationCatalog>();
    public DbSet<MedicationScheme> MedicationSchemes => Set<MedicationScheme>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<DailyMedicationSchedule> DailyMedicationSchedules => Set<DailyMedicationSchedule>();
    public DbSet<MedicationStockMovement> MedicationStockMovements => Set<MedicationStockMovement>();
    public DbSet<PersonAttachment> PersonAttachments => Set<PersonAttachment>();
    public DbSet<Backup> Backups => Set<Backup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VicariaDbContext).Assembly);
        MarkTimestampsAsUtc(modelBuilder);
    }

    // las marcas de tiempo se guardan en UTC; al leerlas se marcan como UTC para que la API
    // las mande con "Z" y el navegador las muestre en hora de Córdoba. Las fechas sin hora
    // (nacimiento, asistencia, evaluación, visitas) no se tocan.
    private static void MarkTimestampsAsUtc(ModelBuilder modelBuilder)
    {
        var utc = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        var nullableUtc = new ValueConverter<DateTime?, DateTime?>(
            value => value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (!IsTimestamp(entityType.ClrType, property.Name))
                {
                    continue;
                }

                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utc);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtc);
                }
            }
        }
    }

    private static bool IsTimestamp(Type entity, string propertyName) =>
        propertyName.EndsWith("At")
        || propertyName is nameof(User.LockoutEnd) or nameof(User.RefreshTokenExpiry)
        || (entity == typeof(AuditLog) && propertyName == nameof(AuditLog.Date))
        || (entity == typeof(MedicationStockMovement) && propertyName == nameof(MedicationStockMovement.Date))
        || (entity == typeof(CasaConvivenciaStay) && propertyName is nameof(CasaConvivenciaStay.EntryDate) or nameof(CasaConvivenciaStay.ExitDate));
}
