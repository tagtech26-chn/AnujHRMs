namespace Attendance.Domain.Entities;

public sealed class TravelRequest
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public DateOnly TravelFrom { get; set; }
    public DateOnly TravelTo { get; set; }
    public string TravelDuration { get; set; } = "SingleDay";
    public string? TravelMode { get; set; }
    public string? VehicleType { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public string? Purpose { get; set; }
    public decimal? EstimatedAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid? ReportingManagerId { get; set; }
    public string? ManagerRemarks { get; set; }
    public Guid? FinanceApproverId { get; set; }
    public string? FinanceRemarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
}
