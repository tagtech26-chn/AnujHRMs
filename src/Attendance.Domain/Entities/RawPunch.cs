namespace Attendance.Domain.Entities;

public sealed class RawPunch
{
    public long Id { get; set; }
    public Guid DeviceId { get; set; }
    public string DeviceUserId { get; set; } = string.Empty;
    public DateTime PunchTime { get; set; }
    public int? PunchState { get; set; }
    public string? VerificationType { get; set; }
    public string? TransactionKey { get; set; }
    public DateTime ImportedAtUtc { get; set; }
}
