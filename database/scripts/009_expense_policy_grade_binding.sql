-- AnujHRMS Expense Policy refinement
-- Migration 009: bind standard rules to grade and allow department/grade exceptions.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.TravelPolicyRules') AND name=N'EmployeeGradeId')
    ALTER TABLE dbo.TravelPolicyRules ADD EmployeeGradeId UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.TravelPolicyExceptions') AND name=N'DepartmentId')
    ALTER TABLE dbo.TravelPolicyExceptions ADD DepartmentId UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.TravelPolicyExceptions') AND name=N'EmployeeGradeId')
    ALTER TABLE dbo.TravelPolicyExceptions ADD EmployeeGradeId UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_TravelPolicyRules_EmployeeGrades')
    ALTER TABLE dbo.TravelPolicyRules ADD CONSTRAINT FK_TravelPolicyRules_EmployeeGrades
        FOREIGN KEY(EmployeeGradeId) REFERENCES dbo.EmployeeGrades(Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_TravelPolicyExceptions_Departments')
    ALTER TABLE dbo.TravelPolicyExceptions ADD CONSTRAINT FK_TravelPolicyExceptions_Departments
        FOREIGN KEY(DepartmentId) REFERENCES dbo.Departments(Id);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_TravelPolicyExceptions_EmployeeGrades')
    ALTER TABLE dbo.TravelPolicyExceptions ADD CONSTRAINT FK_TravelPolicyExceptions_EmployeeGrades
        FOREIGN KEY(EmployeeGradeId) REFERENCES dbo.EmployeeGrades(Id);

-- Bind the seeded standard rules using the grade note.
UPDATE r
SET r.EmployeeGradeId = g.Id
FROM dbo.TravelPolicyRules r
JOIN dbo.EmployeeGrades g
  ON r.Notes LIKE CASE g.GradeCode
        WHEN N'P7' THEN N'P7 - Directors%'
        WHEN N'P6_PLUS' THEN N'P6 & Above%'
        WHEN N'P5_P4' THEN N'P5 & P4%'
        WHEN N'P3' THEN N'P3%'
        WHEN N'P1_P2' THEN N'P1 & P2%'
        WHEN N'P0' THEN N'P0%'
     END
WHERE r.EmployeeGradeId IS NULL;

IF NOT EXISTS (SELECT 1 FROM dbo.__AnujHRMSSchemaVersion WHERE VersionNumber=9)
    INSERT dbo.__AnujHRMSSchemaVersion(VersionNumber,AppliedAt) VALUES(9,SYSUTCDATETIME());

COMMIT TRANSACTION;
