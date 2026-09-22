using Vicaria.Domain.Entities;

namespace Vicaria.Application.CasonaStays;

public class CasonaStayDto
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public StayExitReason? ExitReason { get; set; }
    public string? Reason { get; set; }
    public int DurationDays { get; set; }
    public bool IsActive => ExitDate is null;
}