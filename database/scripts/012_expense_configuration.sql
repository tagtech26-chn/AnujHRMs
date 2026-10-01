-- AnujHRMS Expense configuration
-- Schema version 12
IF NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 12)
BEGIN
    CREATE TABLE dbo.SystemSettings (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        SettingKey nvarchar(150) NOT NULL,
        SettingValue nvarchar(500) NOT NULL,
        Description nvarchar(1000) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_SystemSettings_IsActive DEFAULT(1),
        UpdatedAtUtc datetime2 NOT NULL,
        CONSTRAINT UQ_SystemSettings_SettingKey UNIQUE(SettingKey)
    );

    INSERT INTO dbo.SystemSettings
        (Id, SettingKey, SettingValue, Description, IsActive, UpdatedAtUtc)
    VALUES
        (NEWID(),
         N'Expense.FinanceApproverEmployeeCode',
         N'00097',
         N'Employee code used for Finance approval in travel and expense workflows.',
         1,
         SYSUTCDATETIME());

    INSERT INTO dbo.__AnujHRMSSchemaVersion(VersionNumber, AppliedAt)
    VALUES (12, SYSUTCDATETIME());
END
