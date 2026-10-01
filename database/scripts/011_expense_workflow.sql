-- AnujHRMS Expense / Travel Workflow
-- Schema version 11
IF NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber = 11)
BEGIN
    CREATE TABLE dbo.ExpenseWorkflowHistories (
        Id uniqueidentifier NOT NULL PRIMARY KEY,
        EntityType nvarchar(30) NOT NULL,
        EntityId uniqueidentifier NOT NULL,
        Action nvarchar(40) NOT NULL,
        FromStatus nvarchar(30) NOT NULL,
        ToStatus nvarchar(30) NOT NULL,
        ActorEmployeeId uniqueidentifier NULL,
        Remarks nvarchar(2000) NULL,
        ActionedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_ExpenseWorkflowHistories_Actor FOREIGN KEY(ActorEmployeeId) REFERENCES dbo.Employees(Id)
    );
    CREATE INDEX IX_ExpenseWorkflowHistories_Entity
        ON dbo.ExpenseWorkflowHistories(EntityType, EntityId, ActionedAtUtc);
    INSERT INTO dbo.__AnujHRMSSchemaVersion(VersionNumber, AppliedAt)
    VALUES (11, SYSUTCDATETIME());
END
