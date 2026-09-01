namespace Vicaria.Domain.Entities;

// la ficha social (SCRUM-5): separada de Person a propósito, para poder crear
// a la persona con solo el nombre y completar el resto después
public class SocialRecord
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public SocialRecordStatus Status { get; set; }
    public PersonType? PersonType { get; set; }
    public string? ReasonForEntry { get; set; }
    public DateTime? EntryDate { get; set; }
    public string? HousingSituation { get; set; }
    public string? OvernightLocation { get; set; }
    public string? Occupation { get; set; }
    public bool HasDocumentation { get; set; }
    public string? GeneralNotes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
