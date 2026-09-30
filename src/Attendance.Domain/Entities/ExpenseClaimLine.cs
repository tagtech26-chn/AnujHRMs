namespace Attendance.Domain.Entities;

public sealed class ExpenseClaimLine
{
    public Guid Id { get; set; }
    public Guid ExpenseClaimId { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TravelMode { get; set; }
    public string? VehicleType { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal ClaimedAmount { get; set; }
    public decimal EligibleAmount { get; set; }
    public decimal RejectedAmount { get; set; }
    public string? PolicyRuleType { get; set; }
    public string? ValidationStatus { get; set; }
    public string? ValidationMessage { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool AttachmentProvided { get; set; }
}
