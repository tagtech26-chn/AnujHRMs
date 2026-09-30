namespace Attendance.Domain.Entities;

public sealed class TravelPolicy
{
    public Guid Id { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = "1.0";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
