namespace Attendance.Domain.Entities;

public sealed class Branch
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
