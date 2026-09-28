/*
 AnujHRMS Phase 1 database
 Target: SQL Server 2019+
*/

IF DB_ID(N'AnujHRMS') IS NULL
    CREATE DATABASE [AnujHRMS];
GO

USE [AnujHRMS];
GO

IF OBJECT_ID(N'dbo.__AnujHRMSSchemaVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__AnujHRMSSchemaVersion
    (
        VersionNumber INT NOT NULL,
        AppliedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AnujHRMSSchemaVersion_AppliedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 2)
BEGIN
    CREATE TABLE dbo.Organizations
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Organizations PRIMARY KEY,
        OrganizationCode NVARCHAR(30) NOT NULL,
        OrganizationName NVARCHAR(200) NOT NULL,
        LegalName NVARCHAR(250) NULL,
        TimeZoneId NVARCHAR(100) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Organizations_IsActive DEFAULT 1
    );
    CREATE UNIQUE INDEX UX_Organizations_Code ON dbo.Organizations(OrganizationCode);

    CREATE TABLE dbo.Branches
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Branches PRIMARY KEY,
        OrganizationId UNIQUEIDENTIFIER NOT NULL,
        BranchCode NVARCHAR(30) NOT NULL,
        BranchName NVARCHAR(200) NOT NULL,
        Address NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Branches_IsActive DEFAULT 1,
        CONSTRAINT FK_Branches_Organizations FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id)
    );
    CREATE UNIQUE INDEX UX_Branches_Org_Code ON dbo.Branches(OrganizationId, BranchCode);

    CREATE TABLE dbo.Departments
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Departments PRIMARY KEY,
        OrganizationId UNIQUEIDENTIFIER NOT NULL,
        DepartmentCode NVARCHAR(30) NOT NULL,
        DepartmentName NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT 1,
        CONSTRAINT FK_Departments_Organizations FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id)
    );
    CREATE UNIQUE INDEX UX_Departments_Org_Code ON dbo.Departments(OrganizationId, DepartmentCode);

    CREATE TABLE dbo.Employees
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Employees PRIMARY KEY,
        EmployeeCode NVARCHAR(30) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        DepartmentId UNIQUEIDENTIFIER NULL,
        BranchId UNIQUEIDENTIFIER NULL,
        ReportingManagerId UNIQUEIDENTIFIER NULL,
        JoiningDate DATE NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT 1,
        CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
        CONSTRAINT FK_Employees_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
        CONSTRAINT FK_Employees_Manager FOREIGN KEY (ReportingManagerId) REFERENCES dbo.Employees(Id)
    );
    CREATE UNIQUE INDEX UX_Employees_Code ON dbo.Employees(EmployeeCode);

    CREATE TABLE dbo.RawPunches
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RawPunches PRIMARY KEY,
        DeviceId UNIQUEIDENTIFIER NOT NULL,
        DeviceUserId NVARCHAR(100) NOT NULL,
        PunchTime DATETIME2(0) NOT NULL,
        PunchState INT NULL,
        VerificationType NVARCHAR(50) NULL,
        TransactionKey NVARCHAR(200) NULL,
        ImportedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_RawPunches_ImportedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_RawPunches_Device_User_Time ON dbo.RawPunches(DeviceId, DeviceUserId, PunchTime);
    CREATE UNIQUE INDEX UX_RawPunches_TransactionKey ON dbo.RawPunches(TransactionKey) WHERE TransactionKey IS NOT NULL;

    CREATE TABLE dbo.AttendanceRecords
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AttendanceRecords PRIMARY KEY,
        EmployeeId UNIQUEIDENTIFIER NOT NULL,
        AttendanceDate DATE NOT NULL,
        FirstIn DATETIME2(0) NULL,
        LastOut DATETIME2(0) NULL,
        WorkedMinutes INT NOT NULL CONSTRAINT DF_Attendance_Worked DEFAULT 0,
        LateMinutes INT NOT NULL CONSTRAINT DF_Attendance_Late DEFAULT 0,
        EarlyLeavingMinutes INT NOT NULL CONSTRAINT DF_Attendance_Early DEFAULT 0,
        OvertimeMinutes INT NOT NULL CONSTRAINT DF_Attendance_Overtime DEFAULT 0,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Attendance_Status DEFAULT N'Pending',
        CONSTRAINT FK_Attendance_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id)
    );
    CREATE UNIQUE INDEX UX_Attendance_Employee_Date ON dbo.AttendanceRecords(EmployeeId, AttendanceDate);

    INSERT INTO dbo.__AnujHRMSSchemaVersion(VersionNumber) VALUES (2);
END
GO
