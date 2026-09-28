namespace Attendance.Domain.Entities;

public sealed class Organization
{
    public Guid Id { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TimeZoneId { get; set; }
    public bool IsActive { get; set; } = true;
}
