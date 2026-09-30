SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 7)
BEGIN
    CREATE TABLE dbo.LeaveTypes
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeaveTypes PRIMARY KEY,
        LeaveCode NVARCHAR(30) NOT NULL,
        LeaveName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsPaid BIT NOT NULL CONSTRAINT DF_LeaveTypes_IsPaid DEFAULT (1),
        IsHalfDayAllowed BIT NOT NULL CONSTRAINT DF_LeaveTypes_IsHalfDayAllowed DEFAULT (1),
        RequiresAttachment BIT NOT NULL CONSTRAINT DF_LeaveTypes_RequiresAttachment DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_LeaveTypes_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_LeaveTypes_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_LeaveTypes_LeaveCode UNIQUE (LeaveCode)
    );

    CREATE TABLE dbo.LeavePolicies
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeavePolicies PRIMARY KEY,
        LeaveTypeId UNIQUEIDENTIFIER NOT NULL,
        PolicyName NVARCHAR(150) NOT NULL,
        AccrualType NVARCHAR(20) NOT NULL CONSTRAINT DF_LeavePolicies_AccrualType DEFAULT ('Monthly'),
        MonthlyEntitlement DECIMAL(10,2) NOT NULL CONSTRAINT DF_LeavePolicies_MonthlyEntitlement DEFAULT (0),
        AnnualEntitlement DECIMAL(10,2) NULL,
        CarryForwardAllowed BIT NOT NULL CONSTRAINT DF_LeavePolicies_CarryForwardAllowed DEFAULT (0),
        MaximumCarryForward DECIMAL(10,2) NOT NULL CONSTRAINT DF_LeavePolicies_MaximumCarryForward DEFAULT (0),
        AllowNegativeBalance BIT NOT NULL CONSTRAINT DF_LeavePolicies_AllowNegativeBalance DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_LeavePolicies_IsActive DEFAULT (1),
        EffectiveFrom DATE NOT NULL,
        EffectiveTo DATE NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_LeavePolicies_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_LeavePolicies_LeaveTypes FOREIGN KEY (LeaveTypeId) REFERENCES dbo.LeaveTypes(Id),
        CONSTRAINT CK_LeavePolicies_AccrualType CHECK (AccrualType IN ('Monthly','Annual','None')),
        CONSTRAINT CK_LeavePolicies_MonthlyEntitlement CHECK (MonthlyEntitlement >= 0),
        CONSTRAINT CK_LeavePolicies_AnnualEntitlement CHECK (AnnualEntitlement IS NULL OR AnnualEntitlement >= 0),
        CONSTRAINT CK_LeavePolicies_MaximumCarryForward CHECK (MaximumCarryForward >= 0),
        CONSTRAINT CK_LeavePolicies_NoCarryForward CHECK
        (
            CarryForwardAllowed = 1 OR MaximumCarryForward = 0
        )
    );

    CREATE TABLE dbo.EmployeeLeaveBalances
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EmployeeLeaveBalances PRIMARY KEY,
        EmployeeId UNIQUEIDENTIFIER NOT NULL,
        LeavePolicyId UNIQUEIDENTIFIER NOT NULL,
        BalanceYear INT NOT NULL,
        BalanceMonth INT NOT NULL,
        EntitledDays DECIMAL(10,2) NOT NULL CONSTRAINT DF_EmployeeLeaveBalances_EntitledDays DEFAULT (0),
        AdjustmentDays DECIMAL(10,2) NOT NULL CONSTRAINT DF_EmployeeLeaveBalances_AdjustmentDays DEFAULT (0),
        UsedDays DECIMAL(10,2) NOT NULL CONSTRAINT DF_EmployeeLeaveBalances_UsedDays DEFAULT (0),
        ExpiredDays DECIMAL(10,2) NOT NULL CONSTRAINT DF_EmployeeLeaveBalances_ExpiredDays DEFAULT (0),
        AvailableDays AS (EntitledDays + AdjustmentDays - UsedDays - ExpiredDays) PERSISTED,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_EmployeeLeaveBalances_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_EmployeeLeaveBalances_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_EmployeeLeaveBalances_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT FK_EmployeeLeaveBalances_LeavePolicies FOREIGN KEY (LeavePolicyId) REFERENCES dbo.LeavePolicies(Id),
        CONSTRAINT CK_EmployeeLeaveBalances_Month CHECK (BalanceMonth BETWEEN 1 AND 12),
        CONSTRAINT UQ_EmployeeLeaveBalances UNIQUE (EmployeeId, LeavePolicyId, BalanceYear, BalanceMonth)
    );

    CREATE TABLE dbo.LeaveApplications
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeaveApplications PRIMARY KEY,
        EmployeeId UNIQUEIDENTIFIER NOT NULL,
        LeaveTypeId UNIQUEIDENTIFIER NOT NULL,
        FromDate DATE NOT NULL,
        ToDate DATE NOT NULL,
        FromDayPart NVARCHAR(20) NOT NULL CONSTRAINT DF_LeaveApplications_FromDayPart DEFAULT ('FullDay'),
        ToDayPart NVARCHAR(20) NOT NULL CONSTRAINT DF_LeaveApplications_ToDayPart DEFAULT ('FullDay'),
        TotalDays DECIMAL(10,2) NOT NULL,
        Reason NVARCHAR(1000) NULL,
        AttachmentFileName NVARCHAR(260) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_LeaveApplications_Status DEFAULT ('Pending'),
        AppliedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_LeaveApplications_AppliedAtUtc DEFAULT (SYSUTCDATETIME()),
        ApprovedAtUtc DATETIME2 NULL,
        RejectedAtUtc DATETIME2 NULL,
        CancelledAtUtc DATETIME2 NULL,
        CONSTRAINT FK_LeaveApplications_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT FK_LeaveApplications_LeaveTypes FOREIGN KEY (LeaveTypeId) REFERENCES dbo.LeaveTypes(Id),
        CONSTRAINT CK_LeaveApplications_DateRange CHECK (ToDate >= FromDate),
        CONSTRAINT CK_LeaveApplications_Status CHECK (Status IN ('Pending','Approved','Rejected','Cancelled','Withdrawn')),
        CONSTRAINT CK_LeaveApplications_FromDayPart CHECK (FromDayPart IN ('FullDay','FirstHalf','SecondHalf')),
        CONSTRAINT CK_LeaveApplications_ToDayPart CHECK (ToDayPart IN ('FullDay','FirstHalf','SecondHalf')),
        CONSTRAINT CK_LeaveApplications_TotalDays CHECK (TotalDays > 0)
    );

    CREATE TABLE dbo.LeaveApprovalSteps
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeaveApprovalSteps PRIMARY KEY,
        LeaveApplicationId UNIQUEIDENTIFIER NOT NULL,
        StepNumber INT NOT NULL,
        ApproverEmployeeId UNIQUEIDENTIFIER NULL,
        ApproverRole NVARCHAR(50) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_LeaveApprovalSteps_Status DEFAULT ('Pending'),
        Comments NVARCHAR(1000) NULL,
        ActionedAtUtc DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_LeaveApprovalSteps_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_LeaveApprovalSteps_Applications FOREIGN KEY (LeaveApplicationId) REFERENCES dbo.LeaveApplications(Id),
        CONSTRAINT FK_LeaveApprovalSteps_ApproverEmployee FOREIGN KEY (ApproverEmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT CK_LeaveApprovalSteps_Status CHECK (Status IN ('Pending','Approved','Rejected','Skipped')),
        CONSTRAINT UQ_LeaveApprovalSteps UNIQUE (LeaveApplicationId, StepNumber)
    );

    CREATE TABLE dbo.LeaveBalanceTransactions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LeaveBalanceTransactions PRIMARY KEY,
        EmployeeLeaveBalanceId UNIQUEIDENTIFIER NOT NULL,
        LeaveApplicationId UNIQUEIDENTIFIER NULL,
        TransactionType NVARCHAR(30) NOT NULL,
        TransactionDays DECIMAL(10,2) NOT NULL,
        TransactionDate DATE NOT NULL,
        Remarks NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_LeaveBalanceTransactions_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_LeaveBalanceTransactions_Balance FOREIGN KEY (EmployeeLeaveBalanceId) REFERENCES dbo.EmployeeLeaveBalances(Id),
        CONSTRAINT FK_LeaveBalanceTransactions_Application FOREIGN KEY (LeaveApplicationId) REFERENCES dbo.LeaveApplications(Id),
        CONSTRAINT CK_LeaveBalanceTransactions_Type CHECK
        (
            TransactionType IN ('Accrual','Adjustment','LeaveUsed','LeaveReversal','Expiry')
        )
    );

    CREATE INDEX IX_LeavePolicies_LeaveType_Active
        ON dbo.LeavePolicies(LeaveTypeId, IsActive, EffectiveFrom);

    CREATE INDEX IX_EmployeeLeaveBalances_Employee_Period
        ON dbo.EmployeeLeaveBalances(EmployeeId, BalanceYear, BalanceMonth);

    CREATE INDEX IX_LeaveApplications_Employee_Status
        ON dbo.LeaveApplications(EmployeeId, Status, FromDate);

    CREATE INDEX IX_LeaveApplications_Dates
        ON dbo.LeaveApplications(FromDate, ToDate, Status);

    CREATE INDEX IX_LeaveApprovalSteps_Application_Status
        ON dbo.LeaveApprovalSteps(LeaveApplicationId, Status);

    CREATE INDEX IX_LeaveBalanceTransactions_Balance_Date
        ON dbo.LeaveBalanceTransactions(EmployeeLeaveBalanceId, TransactionDate);

    INSERT INTO dbo.__AnujHRMSSchemaVersion (VersionNumber, AppliedAt)
    VALUES (7, SYSUTCDATETIME());
END
GO
