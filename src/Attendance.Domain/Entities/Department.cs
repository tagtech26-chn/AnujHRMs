namespace Attendance.Domain.Entities;

public sealed class Department
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
