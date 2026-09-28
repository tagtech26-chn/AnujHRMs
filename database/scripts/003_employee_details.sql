/*
 AnujHRMS Phase 1 - Employee details migration
 Adds personal, contact, employment and biometric mapping fields.
 Safe to run multiple times.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [AnujHRMS];
GO

IF COL_LENGTH('dbo.Employees', 'DateOfBirth') IS NULL
    ALTER TABLE dbo.Employees ADD DateOfBirth DATE NULL;
GO
IF COL_LENGTH('dbo.Employees', 'Gender') IS NULL
    ALTER TABLE dbo.Employees ADD Gender NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.Employees', 'MobileNumber') IS NULL
    ALTER TABLE dbo.Employees ADD MobileNumber NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.Employees', 'EmailAddress') IS NULL
    ALTER TABLE dbo.Employees ADD EmailAddress NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.Employees', 'Address') IS NULL
    ALTER TABLE dbo.Employees ADD Address NVARCHAR(1000) NULL;
GO
IF COL_LENGTH('dbo.Employees', 'Designation') IS NULL
    ALTER TABLE dbo.Employees ADD Designation NVARCHAR(150) NULL;
GO
IF COL_LENGTH('dbo.Employees', 'EmploymentType') IS NULL
    ALTER TABLE dbo.Employees ADD EmploymentType NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.Employees', 'ConfirmationDate') IS NULL
    ALTER TABLE dbo.Employees ADD ConfirmationDate DATE NULL;
GO
IF COL_LENGTH('dbo.Employees', 'BiometricUserId') IS NULL
    ALTER TABLE dbo.Employees ADD BiometricUserId NVARCHAR(100) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Employees_BiometricUserId'
      AND object_id = OBJECT_ID(N'dbo.Employees')
)
BEGIN
    CREATE UNIQUE INDEX UX_Employees_BiometricUserId
        ON dbo.Employees(BiometricUserId)
        WHERE BiometricUserId IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.__AnujHRMSSchemaVersion', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 3)
BEGIN
    INSERT INTO dbo.__AnujHRMSSchemaVersion(VersionNumber) VALUES (3);
END
GO
