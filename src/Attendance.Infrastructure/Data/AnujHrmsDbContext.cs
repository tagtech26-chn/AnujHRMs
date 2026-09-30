using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Data;

public sealed class AnujHrmsDbContext(DbContextOptions<AnujHrmsDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeGrade> EmployeeGrades => Set<EmployeeGrade>();
    public DbSet<TravelPolicy> TravelPolicies => Set<TravelPolicy>();
    public DbSet<TravelPolicyRule> TravelPolicyRules => Set<TravelPolicyRule>();
    public DbSet<TravelPolicyException> TravelPolicyExceptions => Set<TravelPolicyException>();
    public DbSet<TravelPolicyExceptionRule> TravelPolicyExceptionRules => Set<TravelPolicyExceptionRule>();
    public DbSet<TravelRequest> TravelRequests => Set<TravelRequest>();
    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();
    public DbSet<ExpenseClaimLine> ExpenseClaimLines => Set<ExpenseClaimLine>();
    public DbSet<ExpenseClaimAttachment> ExpenseClaimAttachments => Set<ExpenseClaimAttachment>();
    public DbSet<RawPunch> RawPunches => Set<RawPunch>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();
    public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();
    public DbSet<LeaveBalanceTransaction> LeaveBalanceTransactions => Set<LeaveBalanceTransaction>();

    // Expense / travel claim mappings are kept explicit so the workflow remains independent of UI values.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e => { e.ToTable("Organizations"); e.HasKey(x => x.Id); e.Property(x => x.OrganizationCode).HasMaxLength(30).IsRequired(); e.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired(); e.Property(x => x.LegalName).HasMaxLength(250); e.Property(x => x.TimeZoneId).HasMaxLength(100); e.HasIndex(x => x.OrganizationCode).IsUnique(); });
        modelBuilder.Entity<Branch>(e => { e.ToTable("Branches"); e.HasKey(x => x.Id); e.Property(x => x.BranchCode).HasMaxLength(30).IsRequired(); e.Property(x => x.BranchName).HasMaxLength(200).IsRequired(); e.Property(x => x.Address).HasMaxLength(500); e.HasIndex(x => new { x.OrganizationId, x.BranchCode }).IsUnique(); });
        modelBuilder.Entity<Department>(e => { e.ToTable("Departments"); e.HasKey(x => x.Id); e.Property(x => x.DepartmentCode).HasMaxLength(30).IsRequired(); e.Property(x => x.DepartmentName).HasMaxLength(200).IsRequired(); e.HasIndex(x => new { x.OrganizationId, x.DepartmentCode }).IsUnique(); });
        modelBuilder.Entity<Employee>(e => { e.ToTable("Employees"); e.HasKey(x => x.Id); e.Property(x => x.EmployeeCode).HasMaxLength(30).IsRequired(); e.Property(x => x.FullName).HasMaxLength(200).IsRequired(); e.Property(x => x.DateOfBirth).HasColumnType("date"); e.Property(x => x.Gender).HasMaxLength(30); e.Property(x => x.MobileNumber).HasMaxLength(30); e.Property(x => x.EmailAddress).HasMaxLength(200); e.Property(x => x.Address).HasMaxLength(1000); e.Property(x => x.EmergencyContactName).HasMaxLength(200); e.Property(x => x.EmergencyContactNumber).HasMaxLength(30); e.Property(x => x.EmergencyContactRelation).HasMaxLength(50); e.Property(x => x.Designation).HasMaxLength(150); e.Property(x => x.EmploymentType).HasMaxLength(50); e.Property(x => x.BiometricUserId).HasMaxLength(100); e.Property(x => x.ProfilePhotoFileName).HasMaxLength(260); e.Property(x => x.ProfilePhotoStoredFileName).HasMaxLength(260); e.Property(x => x.ProfilePhotoContentType).HasMaxLength(150); e.Property(x => x.ProfilePhotoUpdatedAtUtc).HasColumnType("datetime2"); e.HasIndex(x => x.EmployeeCode).IsUnique(); e.HasIndex(x => x.BiometricUserId).IsUnique().HasFilter("[BiometricUserId] IS NOT NULL"); e.Property(x => x.JoiningDate).HasColumnType("date"); e.Property(x => x.ConfirmationDate).HasColumnType("date"); e.HasOne<EmployeeGrade>().WithMany().HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<EmployeeGrade>(e => { e.ToTable("EmployeeGrades"); e.HasKey(x=>x.Id); e.Property(x=>x.GradeCode).HasMaxLength(30).IsRequired(); e.Property(x=>x.GradeName).HasMaxLength(100).IsRequired(); e.Property(x=>x.Description).HasMaxLength(500); e.HasIndex(x=>x.GradeCode).IsUnique(); });
        modelBuilder.Entity<TravelPolicy>(e => { e.ToTable("TravelPolicies"); e.HasKey(x=>x.Id); e.Property(x=>x.PolicyCode).HasMaxLength(50).IsRequired(); e.Property(x=>x.PolicyName).HasMaxLength(150).IsRequired(); e.Property(x=>x.PolicyVersion).HasMaxLength(30).IsRequired(); e.Property(x=>x.EffectiveFrom).HasColumnType("date"); e.Property(x=>x.EffectiveTo).HasColumnType("date"); e.Property(x=>x.CreatedAtUtc).HasColumnType("datetime2"); e.HasIndex(x=>x.PolicyCode).IsUnique(); });
        modelBuilder.Entity<TravelPolicyRule>(e => { e.ToTable("TravelPolicyRules"); e.HasKey(x=>x.Id); e.Property(x=>x.EmployeeGradeId); e.Property(x=>x.RuleType).HasMaxLength(50).IsRequired(); e.Property(x=>x.TravelDuration).HasMaxLength(20).IsRequired(); e.Property(x=>x.TravelMode).HasMaxLength(50); e.Property(x=>x.VehicleType).HasMaxLength(30); e.Property(x=>x.Amount).HasPrecision(12,2); e.Property(x=>x.RatePerKm).HasPrecision(12,2); e.Property(x=>x.MaxKmPerDay).HasPrecision(12,2); e.Property(x=>x.CalculationType).HasMaxLength(50); e.Property(x=>x.Notes).HasMaxLength(1000); e.HasIndex(x=>new{x.TravelPolicyId,x.RuleType,x.TravelDuration,x.TravelMode,x.VehicleType}); e.HasOne<TravelPolicy>().WithMany().HasForeignKey(x=>x.TravelPolicyId).OnDelete(DeleteBehavior.Cascade); e.HasOne<EmployeeGrade>().WithMany().HasForeignKey(x=>x.EmployeeGradeId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<TravelPolicyException>(e => { e.ToTable("TravelPolicyExceptions"); e.HasKey(x=>x.Id); e.Property(x=>x.EmployeeId); e.Property(x=>x.DepartmentId); e.Property(x=>x.EmployeeGradeId); e.Property(x=>x.ExceptionName).HasMaxLength(150).IsRequired(); e.Property(x=>x.Reason).HasMaxLength(1000); e.Property(x=>x.EffectiveFrom).HasColumnType("date"); e.Property(x=>x.EffectiveTo).HasColumnType("date"); e.Property(x=>x.CreatedAtUtc).HasColumnType("datetime2"); e.HasIndex(x=>new{x.EmployeeId,x.DepartmentId,x.EmployeeGradeId,x.EffectiveFrom,x.EffectiveTo}); e.HasOne<Employee>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Cascade); e.HasOne<Department>().WithMany().HasForeignKey(x=>x.DepartmentId).OnDelete(DeleteBehavior.Restrict); e.HasOne<EmployeeGrade>().WithMany().HasForeignKey(x=>x.EmployeeGradeId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<TravelPolicyExceptionRule>(e => { e.ToTable("TravelPolicyExceptionRules"); e.HasKey(x=>x.Id); e.Property(x=>x.RuleType).HasMaxLength(50).IsRequired(); e.Property(x=>x.TravelDuration).HasMaxLength(20).IsRequired(); e.Property(x=>x.TravelMode).HasMaxLength(50); e.Property(x=>x.VehicleType).HasMaxLength(30); e.Property(x=>x.Amount).HasPrecision(12,2); e.Property(x=>x.RatePerKm).HasPrecision(12,2); e.Property(x=>x.MaxKmPerDay).HasPrecision(12,2); e.Property(x=>x.CalculationType).HasMaxLength(50); e.Property(x=>x.Notes).HasMaxLength(1000); e.HasIndex(x=>new{x.TravelPolicyExceptionId,x.RuleType,x.TravelDuration,x.TravelMode,x.VehicleType}); e.HasOne<TravelPolicyException>().WithMany().HasForeignKey(x=>x.TravelPolicyExceptionId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<EmployeeDocument>(e => { e.ToTable("EmployeeDocuments"); e.HasKey(x=>x.Id); e.Property(x=>x.DocumentType).HasMaxLength(100).IsRequired(); e.Property(x=>x.DocumentNumber).HasMaxLength(100); e.Property(x=>x.OriginalFileName).HasMaxLength(260).IsRequired(); e.Property(x=>x.StoredFileName).HasMaxLength(260).IsRequired(); e.Property(x=>x.ContentType).HasMaxLength(150).IsRequired(); e.Property(x=>x.FileSize).IsRequired(); e.Property(x=>x.IssueDate).HasColumnType("date"); e.Property(x=>x.ExpiryDate).HasColumnType("date"); e.Property(x=>x.Remarks).HasMaxLength(1000); e.Property(x=>x.UploadedAtUtc).HasColumnType("datetime2"); e.HasIndex(x=>x.EmployeeId); e.HasIndex(x=>new{x.EmployeeId,x.DocumentType}); e.HasOne<Employee>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<LeaveType>(e => { e.ToTable("LeaveTypes"); e.HasKey(x=>x.Id); e.Property(x=>x.LeaveCode).HasMaxLength(30).IsRequired(); e.Property(x=>x.LeaveName).HasMaxLength(100).IsRequired(); e.Property(x=>x.Description).HasMaxLength(500); e.Property(x=>x.CreatedAtUtc).HasColumnType("datetime2"); e.HasIndex(x=>x.LeaveCode).IsUnique(); });
        modelBuilder.Entity<LeavePolicy>(e => { e.ToTable("LeavePolicies"); e.HasKey(x=>x.Id); e.Property(x=>x.PolicyName).HasMaxLength(150).IsRequired(); e.Property(x=>x.AccrualType).HasMaxLength(20).IsRequired(); e.Property(x=>x.MonthlyEntitlement).HasPrecision(10,2); e.Property(x=>x.AnnualEntitlement).HasPrecision(10,2); e.Property(x=>x.MaximumCarryForward).HasPrecision(10,2); e.Property(x=>x.EffectiveFrom).HasColumnType("date"); e.Property(x=>x.EffectiveTo).HasColumnType("date"); e.Property(x=>x.CreatedAtUtc).HasColumnType("datetime2"); e.HasOne<LeaveType>().WithMany().HasForeignKey(x=>x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x=>new{x.LeaveTypeId,x.IsActive,x.EffectiveFrom}); });
        modelBuilder.Entity<EmployeeLeaveBalance>(e => { e.ToTable("EmployeeLeaveBalances"); e.HasKey(x=>x.Id); e.Property(x=>x.EntitledDays).HasPrecision(10,2); e.Property(x=>x.AdjustmentDays).HasPrecision(10,2); e.Property(x=>x.UsedDays).HasPrecision(10,2); e.Property(x=>x.ExpiredDays).HasPrecision(10,2); e.Ignore(x=>x.AvailableDays); e.Property(x=>x.CreatedAtUtc).HasColumnType("datetime2"); e.Property(x=>x.UpdatedAtUtc).HasColumnType("datetime2"); e.HasIndex(x=>new{x.EmployeeId,x.LeavePolicyId,x.BalanceYear,x.BalanceMonth}).IsUnique(); e.HasOne<Employee>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Cascade); e.HasOne<LeavePolicy>().WithMany().HasForeignKey(x=>x.LeavePolicyId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<LeaveBalanceTransaction>(e => { e.ToTable("LeaveBalanceTransactions"); e.HasKey(x=>x.Id); e.Property(x=>x.TransactionType).HasMaxLength(30).IsRequired(); e.Property(x=>x.TransactionDays).HasPrecision(10,2); e.Property(x=>x.TransactionDate).HasColumnType("date"); e.Property(x=>x.Remarks).HasMaxLength(500); e.Property(x=>x.CreatedAtUtc).HasColumnType("datetime2"); e.HasIndex(x=>new{x.EmployeeLeaveBalanceId,x.TransactionDate}); e.HasOne<EmployeeLeaveBalance>().WithMany().HasForeignKey(x=>x.EmployeeLeaveBalanceId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<RawPunch>(e => { e.ToTable("RawPunches"); e.HasKey(x=>x.Id); e.Property(x=>x.DeviceUserId).HasMaxLength(100).IsRequired(); e.Property(x=>x.VerificationType).HasMaxLength(50); e.Property(x=>x.TransactionKey).HasMaxLength(200); e.HasIndex(x=>new{x.DeviceId,x.DeviceUserId,x.PunchTime}); e.HasIndex(x=>x.TransactionKey).IsUnique().HasFilter("[TransactionKey] IS NOT NULL"); });
        modelBuilder.Entity<AttendanceRecord>(e => { e.ToTable("AttendanceRecords"); e.HasKey(x=>x.Id); e.Property(x=>x.AttendanceDate).HasColumnType("date"); e.Property(x=>x.Status).HasMaxLength(30).IsRequired(); e.HasIndex(x=>new{x.EmployeeId,x.AttendanceDate}).IsUnique(); });
    }
}        modelBuilder.Entity<TravelRequest>(e =>
        {
            e.ToTable("TravelRequests"); e.HasKey(x => x.Id); e.HasIndex(x => x.RequestNumber).IsUnique();
            e.Property(x => x.RequestNumber).HasMaxLength(40).IsRequired(); e.Property(x => x.TravelDuration).HasMaxLength(30).IsRequired();
            e.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Employee>().WithMany().HasForeignKey(x => x.ReportingManagerId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ExpenseClaim>(e =>
        {
            e.ToTable("ExpenseClaims"); e.HasKey(x => x.Id); e.HasIndex(x => x.ClaimNumber).IsUnique();
            e.Property(x => x.ClaimNumber).HasMaxLength(40).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TravelRequest>().WithMany().HasForeignKey(x => x.TravelRequestId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Employee>().WithMany().HasForeignKey(x => x.ReportingManagerId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ExpenseClaimLine>(e =>
        {
            e.ToTable("ExpenseClaimLines"); e.HasKey(x => x.Id); e.Property(x => x.ExpenseType).HasMaxLength(50).IsRequired();
            e.HasOne<ExpenseClaim>().WithMany().HasForeignKey(x => x.ExpenseClaimId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ExpenseClaimAttachment>(e =>
        {
            e.ToTable("ExpenseClaimAttachments"); e.HasKey(x => x.Id);
            e.HasOne<ExpenseClaim>().WithMany().HasForeignKey(x => x.ExpenseClaimId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ExpenseClaimLine>().WithMany().HasForeignKey(x => x.ExpenseClaimLineId).OnDelete(DeleteBehavior.NoAction);
        });

