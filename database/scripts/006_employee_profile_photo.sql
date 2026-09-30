/*
 AnujHRMS - Employee profile photo migration
 Safe to run multiple times.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [AnujHRMS];
GO

IF COL_LENGTH(N'dbo.Employees', N'ProfilePhotoFileName') IS NULL
BEGIN
    ALTER TABLE dbo.Employees
        ADD ProfilePhotoFileName NVARCHAR(260) NULL;
END
GO

IF COL_LENGTH(N'dbo.Employees', N'ProfilePhotoStoredFileName') IS NULL
BEGIN
    ALTER TABLE dbo.Employees
        ADD ProfilePhotoStoredFileName NVARCHAR(260) NULL;
END
GO

IF COL_LENGTH(N'dbo.Employees', N'ProfilePhotoContentType') IS NULL
BEGIN
    ALTER TABLE dbo.Employees
        ADD ProfilePhotoContentType NVARCHAR(150) NULL;
END
GO

IF COL_LENGTH(N'dbo.Employees', N'ProfilePhotoFileSize') IS NULL
BEGIN
    ALTER TABLE dbo.Employees
        ADD ProfilePhotoFileSize BIGINT NULL;
END
GO

IF COL_LENGTH(N'dbo.Employees', N'ProfilePhotoUpdatedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Employees
        ADD ProfilePhotoUpdatedAtUtc DATETIME2 NULL;
END
GO

IF OBJECT_ID(N'dbo.__AnujHRMSSchemaVersion', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.__AnujHRMSSchemaVersion
       WHERE VersionNumber = 6
   )
BEGIN
    INSERT INTO dbo.__AnujHRMSSchemaVersion(VersionNumber)
    VALUES (6);
END
GO
