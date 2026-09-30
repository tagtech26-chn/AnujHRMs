namespace Attendance.Domain.Entities;

public sealed class Employee
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    // Personal details
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    // Contact details
    public string? MobileNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactNumber { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // Employment details
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public string? Designation { get; set; }
    public string? EmploymentType { get; set; }
    public DateOnly JoiningDate { get; set; }
    public DateOnly? ConfirmationDate { get; set; }

    // Biometric/device mapping
    public string? BiometricUserId { get; set; }

    // Profile photo metadata. The image itself is stored on the server filesystem.
    public string? ProfilePhotoFileName { get; set; }
    public string? ProfilePhotoStoredFileName { get; set; }
    public string? ProfilePhotoContentType { get; set; }
    public long? ProfilePhotoFileSize { get; set; }
    public DateTime? ProfilePhotoUpdatedAtUtc { get; set; }

    public bool IsActive { get; set; } = true;
}
