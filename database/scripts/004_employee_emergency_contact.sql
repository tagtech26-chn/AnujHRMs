/*
 AnujHRMS - Employee emergency contact migration
 Safe to run multiple times.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [AnujHRMS];
GO

IF COL_LENGTH('dbo.Employees', 'EmergencyContactName') IS NULL
    ALTER TABLE dbo.Employees ADD EmergencyContactName NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.Employees', 'EmergencyContactNumber') IS NULL
    ALTER TABLE dbo.Employees ADD EmergencyContactNumber NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.Employees', 'EmergencyContactRelation') IS NULL
    ALTER TABLE dbo.Employees ADD EmergencyContactRelation NVARCHAR(50) NULL;
GO

IF OBJECT_ID(N'dbo.__AnujHRMSSchemaVersion', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 4)
BEGIN
    INSERT INTO dbo.__AnujHRMSSchemaVersion(VersionNumber) VALUES (4);
END
GO
