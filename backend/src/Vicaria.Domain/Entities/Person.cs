namespace Vicaria.Domain.Entities;

// registro mínimo de identidad; solo FirstName es obligatorio (ver SCRUM-5,
// no se puede exigir documentación para registrar a alguien)
public class Person
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Dni { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}
