namespace Attendance.Domain.Entities;

public sealed class LeavePolicy
{
    public Guid Id { get; set; }
    public Guid LeaveTypeId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string AccrualType { get; set; } = "Monthly";
    public decimal MonthlyEntitlement { get; set; }
    public decimal? AnnualEntitlement { get; set; }
    public bool CarryForwardAllowed { get; set; }
    public decimal MaximumCarryForward { get; set; }
    public bool AllowNegativeBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
