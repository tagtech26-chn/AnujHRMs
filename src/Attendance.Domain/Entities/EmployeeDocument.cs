namespace Attendance.Domain.Entities;

public sealed class EmployeeDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}