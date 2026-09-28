using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Data;

public sealed class AnujHrmsDbContext(DbContextOptions<AnujHrmsDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RawPunch> RawPunches => Set<RawPunch>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e => {
            e.ToTable("Organizations"); e.HasKey(x => x.Id);
            e.Property(x => x.OrganizationCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired();
            e.Property(x => x.LegalName).HasMaxLength(250); e.Property(x => x.TimeZoneId).HasMaxLength(100);
            e.HasIndex(x => x.OrganizationCode).IsUnique();
        });
        modelBuilder.Entity<Branch>(e => {
            e.ToTable("Branches"); e.HasKey(x => x.Id);
            e.Property(x => x.BranchCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.BranchName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Address).HasMaxLength(500);
            e.HasIndex(x => new { x.OrganizationId, x.BranchCode }).IsUnique();
        });
        modelBuilder.Entity<Department>(e => {
            e.ToTable("Departments"); e.HasKey(x => x.Id);
            e.Property(x => x.DepartmentCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.DepartmentName).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.OrganizationId, x.DepartmentCode }).IsUnique();
        });
        modelBuilder.Entity<Employee>(e => {
            e.ToTable("Employees"); e.HasKey(x => x.Id);
            e.Property(x => x.EmployeeCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.DateOfBirth).HasColumnType("date");
            e.Property(x => x.Gender).HasMaxLength(30);
            e.Property(x => x.MobileNumber).HasMaxLength(30);
            e.Property(x => x.EmailAddress).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(1000);
            e.Property(x => x.EmergencyContactName).HasMaxLength(200);
            e.Property(x => x.EmergencyContactNumber).HasMaxLength(30);
            e.Property(x => x.EmergencyContactRelation).HasMaxLength(50);
            e.Property(x => x.Designation).HasMaxLength(150);
            e.Property(x => x.EmploymentType).HasMaxLength(50);
            e.Property(x => x.BiometricUserId).HasMaxLength(100);
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.HasIndex(x => x.BiometricUserId).IsUnique().HasFilter("[BiometricUserId] IS NOT NULL");
            e.Property(x => x.JoiningDate).HasColumnType("date");
            e.Property(x => x.ConfirmationDate).HasColumnType("date");
        });
        modelBuilder.Entity<RawPunch>(e => {
            e.ToTable("RawPunches"); e.HasKey(x => x.Id);
            e.Property(x => x.DeviceUserId).HasMaxLength(100).IsRequired();
            e.Property(x => x.VerificationType).HasMaxLength(50);
            e.Property(x => x.TransactionKey).HasMaxLength(200);
            e.HasIndex(x => new { x.DeviceId, x.DeviceUserId, x.PunchTime });
            e.HasIndex(x => x.TransactionKey).IsUnique().HasFilter("[TransactionKey] IS NOT NULL");
        });
        modelBuilder.Entity<AttendanceRecord>(e => {
            e.ToTable("AttendanceRecords"); e.HasKey(x => x.Id);
            e.Property(x => x.AttendanceDate).HasColumnType("date");
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique();
        });
    }
}