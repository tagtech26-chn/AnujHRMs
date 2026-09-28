/*
 AnujHRMS database foundation
 Target: SQL Server 2019+
 Run against the target SQL Server instance.
*/

IF DB_ID(N'AnujHRMS') IS NULL
BEGIN
    CREATE DATABASE [AnujHRMS];
END;
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

    INSERT INTO dbo.__AnujHRMSSchemaVersion (VersionNumber)
    VALUES (1);
END;
GO
