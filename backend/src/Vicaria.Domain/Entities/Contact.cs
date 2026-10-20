namespace Vicaria.Domain.Entities;

// contacto de referencia familiar, opcional (SCRUM-5)
public class Contact
{
    public Guid Id { get; set; }
    public Guid SocialRecordId { get; set; }
    public SocialRecord? SocialRecord { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
