namespace Attendance.Domain.Entities;

public sealed class ExpenseClaimAttachment
{
    public Guid Id { get; set; }
    public Guid ExpenseClaimId { get; set; }
    public Guid? ExpenseClaimLineId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
