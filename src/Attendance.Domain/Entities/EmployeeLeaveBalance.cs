namespace Attendance.Domain.Entities;

public sealed class EmployeeLeaveBalance
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeavePolicyId { get; set; }
    public int BalanceYear { get; set; }
    public int BalanceMonth { get; set; }
    public decimal EntitledDays { get; set; }
    public decimal AdjustmentDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal ExpiredDays { get; set; }
    public decimal AvailableDays => EntitledDays + AdjustmentDays - UsedDays - ExpiredDays;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
