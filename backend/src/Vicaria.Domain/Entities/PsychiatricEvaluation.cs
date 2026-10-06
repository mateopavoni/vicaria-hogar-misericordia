namespace Vicaria.Domain.Entities;

// evaluación psiquiátrica de una persona (SCRUM-134/EP-12): una persona puede
// tener varias evaluaciones, registradas por distintos usuarios. Para que una
// persona pueda ser Residente debe existir una evaluación vigente (IsValid).
public class PsychiatricEvaluation
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public DateTime Date { get; set; }
    public string? Professional { get; set; }
    public string? Diagnosis { get; set; }
    public bool IsValid { get; set; }
    public Guid RegisteredByUserId { get; set; }
    public User? RegisteredBy { get; set; }
}