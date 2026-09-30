namespace Attendance.Domain.Entities;

public sealed class LeaveBalanceTransaction
{
    public Guid Id { get; set; }
    public Guid EmployeeLeaveBalanceId { get; set; }
    public Guid? LeaveApplicationId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal TransactionDays { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
