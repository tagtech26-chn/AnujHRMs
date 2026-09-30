namespace Attendance.Domain.Entities;

public sealed class LeaveType
{
    public Guid Id { get; set; }
    public string LeaveCode { get; set; } = string.Empty;
    public string LeaveName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool IsHalfDayAllowed { get; set; } = true;
    public bool RequiresAttachment { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
