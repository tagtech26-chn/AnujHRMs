namespace Attendance.Domain.Entities;

public sealed class AttendanceRecord
{
    public long Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTime? FirstIn { get; set; }
    public DateTime? LastOut { get; set; }
    public int WorkedMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeavingMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string Status { get; set; } = "Pending";
}
