/*
 AnujHRMS - Employee documents migration
 Safe to run multiple times.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [AnujHRMS];
GO

IF OBJECT_ID(N'dbo.EmployeeDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeDocuments
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EmployeeDocuments PRIMARY KEY,
        EmployeeId UNIQUEIDENTIFIER NOT NULL,
        DocumentType NVARCHAR(100) NOT NULL,
        DocumentNumber NVARCHAR(100) NULL,
        OriginalFileName NVARCHAR(260) NOT NULL,
        StoredFileName NVARCHAR(260) NOT NULL,
        ContentType NVARCHAR(150) NOT NULL,
        FileSize BIGINT NOT NULL,
        IssueDate DATE NULL,
        ExpiryDate DATE NULL,
        Remarks NVARCHAR(1000) NULL,
        UploadedAtUtc DATETIME2 NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_EmployeeDocuments_IsActive DEFAULT (1),
        CONSTRAINT FK_EmployeeDocuments_Employees
            FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id)
    );

    CREATE INDEX IX_EmployeeDocuments_EmployeeId
        ON dbo.EmployeeDocuments(EmployeeId);

    CREATE INDEX IX_EmployeeDocuments_EmployeeId_DocumentType
        ON dbo.EmployeeDocuments(EmployeeId, DocumentType);
END
GO

IF OBJECT_ID(N'dbo.__AnujHRMSSchemaVersion', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 5)
BEGIN
    INSERT INTO dbo.__AnujHRMSSchemaVersion(VersionNumber) VALUES (5);
END
GO