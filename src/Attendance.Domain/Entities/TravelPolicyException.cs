namespace Attendance.Domain.Entities;

public sealed class TravelPolicyException
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string ExceptionName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
