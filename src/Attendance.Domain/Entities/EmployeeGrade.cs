namespace Attendance.Domain.Entities;

public sealed class EmployeeGrade
{
    public Guid Id { get; set; }
    public string GradeCode { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
