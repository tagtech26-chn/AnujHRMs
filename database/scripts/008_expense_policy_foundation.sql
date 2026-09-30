-- AnujHRMS Expense Policy Foundation
-- Migration 008
-- Source: Aravind Ceramics Domestic Travel Policy Ver 1.10, August 2025.
-- This script creates configurable grade/policy/rule/exception masters.
-- It does not create expense claims yet.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Employees') AND name = N'GradeId')
BEGIN
    ALTER TABLE dbo.Employees ADD GradeId UNIQUEIDENTIFIER NULL;
END;

IF OBJECT_ID(N'dbo.EmployeeGrades','U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeGrades
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        GradeCode NVARCHAR(30) NOT NULL,
        GradeName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_EmployeeGrades_IsActive DEFAULT (1),
        CONSTRAINT UQ_EmployeeGrades_GradeCode UNIQUE (GradeCode)
    );
END;

IF OBJECT_ID(N'dbo.TravelPolicies','U') IS NULL
BEGIN
    CREATE TABLE dbo.TravelPolicies
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        PolicyCode NVARCHAR(50) NOT NULL,
        PolicyName NVARCHAR(150) NOT NULL,
        PolicyVersion NVARCHAR(30) NOT NULL,
        EffectiveFrom DATE NOT NULL,
        EffectiveTo DATE NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TravelPolicies_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TravelPolicies_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_TravelPolicies_PolicyCode UNIQUE (PolicyCode)
    );
END;

IF OBJECT_ID(N'dbo.TravelPolicyRules','U') IS NULL
BEGIN
    CREATE TABLE dbo.TravelPolicyRules
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TravelPolicyId UNIQUEIDENTIFIER NOT NULL,
        RuleType NVARCHAR(50) NOT NULL,
        TravelDuration NVARCHAR(20) NOT NULL CONSTRAINT DF_TravelPolicyRules_Duration DEFAULT ('All'),
        TravelMode NVARCHAR(50) NULL,
        VehicleType NVARCHAR(30) NULL,
        Amount DECIMAL(12,2) NULL,
        RatePerKm DECIMAL(12,2) NULL,
        MaxKmPerDay DECIMAL(12,2) NULL,
        CalculationType NVARCHAR(50) NULL,
        RequiresAttachment BIT NOT NULL CONSTRAINT DF_TravelPolicyRules_Attachment DEFAULT (0),
        Notes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TravelPolicyRules_IsActive DEFAULT (1),
        CONSTRAINT FK_TravelPolicyRules_Policy FOREIGN KEY (TravelPolicyId) REFERENCES dbo.TravelPolicies(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_TravelPolicyRules_Lookup
        ON dbo.TravelPolicyRules(TravelPolicyId, RuleType, TravelDuration, TravelMode, VehicleType);
END;

IF OBJECT_ID(N'dbo.TravelPolicyExceptions','U') IS NULL
BEGIN
    CREATE TABLE dbo.TravelPolicyExceptions
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        EmployeeId UNIQUEIDENTIFIER NOT NULL,
        ExceptionName NVARCHAR(150) NOT NULL,
        Reason NVARCHAR(1000) NULL,
        EffectiveFrom DATE NOT NULL,
        EffectiveTo DATE NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TravelPolicyExceptions_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TravelPolicyExceptions_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_TravelPolicyExceptions_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_TravelPolicyExceptions_EmployeeDates
        ON dbo.TravelPolicyExceptions(EmployeeId, EffectiveFrom, EffectiveTo);
END;

IF OBJECT_ID(N'dbo.TravelPolicyExceptionRules','U') IS NULL
BEGIN
    CREATE TABLE dbo.TravelPolicyExceptionRules
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TravelPolicyExceptionId UNIQUEIDENTIFIER NOT NULL,
        RuleType NVARCHAR(50) NOT NULL,
        TravelDuration NVARCHAR(20) NOT NULL CONSTRAINT DF_TravelPolicyExceptionRules_Duration DEFAULT ('All'),
        TravelMode NVARCHAR(50) NULL,
        VehicleType NVARCHAR(30) NULL,
        Amount DECIMAL(12,2) NULL,
        RatePerKm DECIMAL(12,2) NULL,
        MaxKmPerDay DECIMAL(12,2) NULL,
        CalculationType NVARCHAR(50) NULL,
        RequiresAttachment BIT NOT NULL CONSTRAINT DF_TravelPolicyExceptionRules_Attachment DEFAULT (0),
        Notes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TravelPolicyExceptionRules_IsActive DEFAULT (1),
        CONSTRAINT FK_TravelPolicyExceptionRules_Exception FOREIGN KEY (TravelPolicyExceptionId) REFERENCES dbo.TravelPolicyExceptions(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_TravelPolicyExceptionRules_Lookup
        ON dbo.TravelPolicyExceptionRules(TravelPolicyExceptionId, RuleType, TravelDuration, TravelMode, VehicleType);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Employees_EmployeeGrades_GradeId')
BEGIN
    ALTER TABLE dbo.Employees
      ADD CONSTRAINT FK_Employees_EmployeeGrades_GradeId
      FOREIGN KEY (GradeId) REFERENCES dbo.EmployeeGrades(Id);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.EmployeeGrades)
BEGIN
    INSERT dbo.EmployeeGrades(Id, GradeCode, GradeName, Description)
    VALUES
    (NEWID(), N'P7',       N'P7 - Directors', N'Directors (MD/ED/JD)'),
    (NEWID(), N'P6_PLUS',  N'P6 & Above - GM', N'General Manager and applicable P6 and above employees'),
    (NEWID(), N'P5_P4',    N'P5 & P4 - AGM/DGM', N'AGM / DGM'),
    (NEWID(), N'P3',       N'P3 - Branch Manager/Sr. Executives', N'Branch Manager / Senior Executives'),
    (NEWID(), N'P1_P2',    N'P1 & P2 - Executives', N'Junior Executives / Executives'),
    (NEWID(), N'P0',       N'P0 - Trainee/Temporary', N'Trainee / Temporary');
END;

DECLARE @PolicyId UNIQUEIDENTIFIER = (SELECT Id FROM dbo.TravelPolicies WHERE PolicyCode = N'DOMESTIC_TRAVEL' AND PolicyVersion = N'1.10');

IF @PolicyId IS NULL
BEGIN
    SET @PolicyId = NEWID();
    INSERT dbo.TravelPolicies(Id, PolicyCode, PolicyName, PolicyVersion, EffectiveFrom)
    VALUES(@PolicyId, N'DOMESTIC_TRAVEL', N'Domestic Travel Policy', N'1.10', '2025-08-01');

    -- Travel mode allowances by grade band.
    INSERT dbo.TravelPolicyRules(Id,TravelPolicyId,RuleType,TravelDuration,TravelMode,Notes)
    VALUES
    (NEWID(),@PolicyId,N'TravelMode',N'All',N'Air / Train AC-I',N'P7 - Directors'),
    (NEWID(),@PolicyId,N'TravelMode',N'All',N'Train AC-I / Overnight Bus',N'P6 & Above - GM'),
    (NEWID(),@PolicyId,N'TravelMode',N'All',N'Train AC-II / Overnight Bus',N'P5 & P4 - AGM/DGM'),
    (NEWID(),@PolicyId,N'TravelMode',N'All',N'Train AC-II / Overnight Bus',N'P3 - Branch Manager/Sr. Executives'),
    (NEWID(),@PolicyId,N'TravelMode',N'All',N'Train Sleeper / AC-III / Overnight Bus',N'P1 & P2 - Jr. Executives/Executives'),
    (NEWID(),@PolicyId,N'TravelMode',N'All',N'Train Sleeper / Overnight Bus',N'P0 - Trainee/Temporary');

    -- Lodging, food and own-arrangement limits.
    INSERT dbo.TravelPolicyRules(Id,TravelPolicyId,RuleType,TravelDuration,Amount,CalculationType,Notes)
    VALUES
    (NEWID(),@PolicyId,N'Lodging',N'All',2250,N'PerDay',N'P6 & Above'),
    (NEWID(),@PolicyId,N'Lodging',N'All',1200,N'PerDay',N'P5 & P4'),
    (NEWID(),@PolicyId,N'Lodging',N'All',1000,N'PerDay',N'P3'),
    (NEWID(),@PolicyId,N'Lodging',N'All',800,N'PerDay',N'P1 & P2'),
    (NEWID(),@PolicyId,N'Lodging',N'All',700,N'PerDay',N'P0'),
    (NEWID(),@PolicyId,N'Food',N'All',500,N'PerDay',N'P5 & P4'),
    (NEWID(),@PolicyId,N'Food',N'All',400,N'PerDay',N'P3'),
    (NEWID(),@PolicyId,N'Food',N'All',400,N'PerDay',N'P1 & P2'),
    (NEWID(),@PolicyId,N'Food',N'All',350,N'PerDay',N'P0'),
    (NEWID(),@PolicyId,N'OwnArrangement',N'All',1000,N'PerDay',N'P6 & Above'),
    (NEWID(),@PolicyId,N'OwnArrangement',N'All',1000,N'PerDay',N'P5 & P4'),
    (NEWID(),@PolicyId,N'OwnArrangement',N'All',700,N'PerDay',N'P3'),
    (NEWID(),@PolicyId,N'OwnArrangement',N'All',450,N'PerDay',N'P1 & P2'),
    (NEWID(),@PolicyId,N'OwnArrangement',N'All',400,N'PerDay',N'P0');

    -- Vehicle/local rules explicitly stated in the policy.
    INSERT dbo.TravelPolicyRules(Id,TravelPolicyId,RuleType,TravelDuration,VehicleType,Amount,RatePerKm,MaxKmPerDay,CalculationType,Notes)
    VALUES
    (NEWID(),@PolicyId,N'KilometerLimit',N'All',N'Bike',NULL,NULL,250,N'PerDay',N'Own bike domestic travel'),
    (NEWID(),@PolicyId,N'KilometerLimit',N'All',N'Car',NULL,NULL,500,N'PerDay',N'Own car domestic travel for P3 and above'),
    (NEWID(),@PolicyId,N'KilometerRate',N'Local',N'Bike',NULL,2.50,150,N'PerKm',N'Local bike'),
    (NEWID(),@PolicyId,N'KilometerRate',N'Local',N'Car',NULL,6.00,250,N'PerKm',N'Local car'),
    (NEWID(),@PolicyId,N'Food',N'Local',NULL,400,NULL,NULL,N'PerDay',N'Local food maximum');

    INSERT dbo.TravelPolicyRules(Id,TravelPolicyId,RuleType,TravelDuration,RequiresAttachment,Notes)
    VALUES
    (NEWID(),@PolicyId,N'AttachmentRequired',N'All',1,N'Attachment is mandatory for expense claims');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 8)
    INSERT dbo.__AnujHRMSSchemaVersion(VersionNumber, AppliedAt) VALUES (8, SYSUTCDATETIME());

COMMIT TRANSACTION;
