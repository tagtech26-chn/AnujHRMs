namespace Attendance.Domain.Entities;

public sealed class ExpenseClaim
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? TravelRequestId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public DateOnly ClaimDate { get; set; }
    public DateOnly TravelFrom { get; set; }
    public DateOnly TravelTo { get; set; }
    public string TravelDuration { get; set; } = "SingleDay";
    public string Status { get; set; } = "Draft";
    public decimal TotalClaimedAmount { get; set; }
    public decimal TotalEligibleAmount { get; set; }
    public decimal TotalRejectedAmount { get; set; }
    public string? EmployeeRemarks { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public string? ManagerRemarks { get; set; }
    public Guid? FinanceApproverId { get; set; }
    public string? FinanceRemarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? SettledAtUtc { get; set; }
}
