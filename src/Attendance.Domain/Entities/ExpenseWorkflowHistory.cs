namespace Attendance.Domain.Entities;

public sealed class ExpenseWorkflowHistory
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public Guid? ActorEmployeeId { get; set; }
    public string? Remarks { get; set; }
    public DateTime ActionedAtUtc { get; set; }
}
