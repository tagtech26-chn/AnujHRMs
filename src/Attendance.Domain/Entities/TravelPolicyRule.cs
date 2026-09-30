namespace Attendance.Domain.Entities;

public sealed class TravelPolicyRule
{
    public Guid Id { get; set; }
    public Guid TravelPolicyId { get; set; }
    public Guid? EmployeeGradeId { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string TravelDuration { get; set; } = "All";
    public string? TravelMode { get; set; }
    public string? VehicleType { get; set; }
    public decimal? Amount { get; set; }
    public decimal? RatePerKm { get; set; }
    public decimal? MaxKmPerDay { get; set; }
    public string? CalculationType { get; set; }
    public bool RequiresAttachment { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
