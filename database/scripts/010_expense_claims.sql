-- AnujHRMS Expense / Travel Claims
-- Schema version 10
IF NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 10)
BEGIN
    CREATE TABLE dbo.TravelRequests (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        EmployeeId uniqueidentifier NOT NULL,
        RequestNumber nvarchar(40) NOT NULL UNIQUE,
        TravelFrom date NOT NULL,
        TravelTo date NOT NULL,
        TravelDuration nvarchar(30) NOT NULL,
        TravelMode nvarchar(50) NULL,
        VehicleType nvarchar(50) NULL,
        FromLocation nvarchar(250) NULL,
        ToLocation nvarchar(250) NULL,
        Purpose nvarchar(1000) NULL,
        EstimatedAmount decimal(18,2) NULL,
        Status nvarchar(30) NOT NULL,
        ReportingManagerId uniqueidentifier NULL,
        ManagerRemarks nvarchar(2000) NULL,
        FinanceApproverId uniqueidentifier NULL,
        FinanceRemarks nvarchar(2000) NULL,
        CreatedAtUtc datetime2 NOT NULL,
        SubmittedAtUtc datetime2 NULL,
        ApprovedAtUtc datetime2 NULL,
        CONSTRAINT FK_TravelRequests_Employee FOREIGN KEY(EmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT FK_TravelRequests_Manager FOREIGN KEY(ReportingManagerId) REFERENCES dbo.Employees(Id)
    );
    CREATE TABLE dbo.ExpenseClaims (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        EmployeeId uniqueidentifier NOT NULL,
        TravelRequestId uniqueidentifier NULL,
        ClaimNumber nvarchar(40) NOT NULL UNIQUE,
        ClaimDate date NOT NULL,
        TravelFrom date NOT NULL,
        TravelTo date NOT NULL,
        TravelDuration nvarchar(30) NOT NULL,
        Status nvarchar(30) NOT NULL,
        TotalClaimedAmount decimal(18,2) NOT NULL,
        TotalEligibleAmount decimal(18,2) NOT NULL,
        TotalRejectedAmount decimal(18,2) NOT NULL,
        EmployeeRemarks nvarchar(2000) NULL,
        ReportingManagerId uniqueidentifier NULL,
        ManagerRemarks nvarchar(2000) NULL,
        FinanceApproverId uniqueidentifier NULL,
        FinanceRemarks nvarchar(2000) NULL,
        CreatedAtUtc datetime2 NOT NULL,
        SubmittedAtUtc datetime2 NULL,
        ApprovedAtUtc datetime2 NULL,
        SettledAtUtc datetime2 NULL,
        CONSTRAINT FK_ExpenseClaims_Employee FOREIGN KEY(EmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT FK_ExpenseClaims_TravelRequest FOREIGN KEY(TravelRequestId) REFERENCES dbo.TravelRequests(Id),
        CONSTRAINT FK_ExpenseClaims_Manager FOREIGN KEY(ReportingManagerId) REFERENCES dbo.Employees(Id)
    );
    CREATE TABLE dbo.ExpenseClaimLines (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        ExpenseClaimId uniqueidentifier NOT NULL,
        ExpenseDate date NOT NULL,
        ExpenseType nvarchar(50) NOT NULL,
        Description nvarchar(1000) NULL,
        TravelMode nvarchar(50) NULL,
        VehicleType nvarchar(50) NULL,
        DistanceKm decimal(18,2) NULL,
        ClaimedAmount decimal(18,2) NOT NULL,
        EligibleAmount decimal(18,2) NOT NULL,
        RejectedAmount decimal(18,2) NOT NULL,
        PolicyRuleType nvarchar(100) NULL,
        ValidationStatus nvarchar(30) NULL,
        ValidationMessage nvarchar(2000) NULL,
        RequiresAttachment bit NOT NULL,
        AttachmentProvided bit NOT NULL
        ,CONSTRAINT FK_ExpenseClaimLines_Claim FOREIGN KEY(ExpenseClaimId) REFERENCES dbo.ExpenseClaims(Id)
    );
    CREATE TABLE dbo.ExpenseClaimAttachments (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        ExpenseClaimId uniqueidentifier NOT NULL,
        ExpenseClaimLineId uniqueidentifier NULL,
        OriginalFileName nvarchar(260) NOT NULL,
        StoredFileName nvarchar(260) NOT NULL,
        ContentType nvarchar(100) NOT NULL,
        FileSize bigint NOT NULL,
        UploadedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_ExpenseClaimAttachments_Claim FOREIGN KEY(ExpenseClaimId) REFERENCES dbo.ExpenseClaims(Id),
        CONSTRAINT FK_ExpenseClaimAttachments_Line FOREIGN KEY(ExpenseClaimLineId) REFERENCES dbo.ExpenseClaimLines(Id)
    );
    INSERT INTO dbo.__AnujHRMSSchemaVersion(VersionNumber, AppliedAt) VALUES (10, SYSUTCDATETIME());
END
