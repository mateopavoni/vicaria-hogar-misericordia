namespace Vicaria.Domain.Entities;
public class DailyMedicationSchedule
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public Guid MedicationSchemeId { get; set; }
    public MedicationScheme? MedicationScheme { get; set; }
    public Guid MedicationId { get; set; }
    public MedicationCatalog? Medication { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string Dose { get; set; } = string.Empty;
    public MedicationIntakeStatus Status { get; set; } = MedicationIntakeStatus.Pending;
    public string? Observation { get; set; }
    public DateTime? AdministeredAt { get; set; }
    public Guid? AdministeredByUserId { get; set; }
    public User? AdministeredByUser { get; set; }
}