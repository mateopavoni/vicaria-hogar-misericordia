namespace Vicaria.Domain.Entities;

// esquema de medicación de una persona (SCRUM-232/EP-11, historia SCRUM-59):
// qué medicamento, en qué dosis y a qué hora. Se carga en el momento
// (LoadedAt siempre server-side UTC, sin fechas futuras ni pasadas); la
// repetición sale del horario predefinido (Time). IsActive distingue los
// esquemas vigentes de los históricos. PendingStock indica que al cargarlo
// no había stock en farmacia (ver SCRUM-233).
public class MedicationScheme
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public Guid MedicationId { get; set; }
    public MedicationCatalog? Medication { get; set; }
    public string Dose { get; set; } = string.Empty;
    public TimeSpan Time { get; set; }
    public DateTime LoadedAt { get; set; }
    public Guid LoadedByUserId { get; set; }
    public User? LoadedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool PendingStock { get; set; }
}
