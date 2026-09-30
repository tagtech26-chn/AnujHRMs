using System.Globalization;
using System.Text;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var query = db.Employees.AsNoTracking();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return Ok(await query.OrderBy(x => x.EmployeeCode).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Employee>> Get(Guid id, CancellationToken ct)
    {
        var item = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> Create(Employee input, CancellationToken ct)
    {
        if (input.Id == Guid.Empty) input.Id = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(input.EmployeeCode) || string.IsNullOrWhiteSpace(input.FullName))
            return BadRequest("EmployeeCode and FullName are required.");
        if (await db.Employees.AnyAsync(x => x.EmployeeCode == input.EmployeeCode, ct))
            return Conflict("EmployeeCode already exists.");
        var validation = await ValidateReferences(input, ct);
        if (validation is not null) return validation;
        db.Employees.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPost("bulk-upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> BulkUpload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Please select a CSV file." });

        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only CSV files are supported." });

        var lines = new List<string>();
        await using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            while (!reader.EndOfStream)
                lines.Add(await reader.ReadLineAsync(ct) ?? string.Empty);
        }

        if (lines.Count < 2)
            return BadRequest(new { message = "The CSV must contain a header and at least one employee row." });

        var headers = ParseCsvLine(lines[0]);
        var requiredHeaders = new[]
        {
            "OrganizationCode", "EmployeeCode", "FullName", "BranchCode",
            "DepartmentCode", "ReportingManagerCode", "JoiningDate", "IsActive"
        };

        var normalizedHeaders = headers.Select(NormalizeHeader).ToArray();
        if (requiredHeaders.Any(h => !normalizedHeaders.Contains(h, StringComparer.OrdinalIgnoreCase)))
            return BadRequest(new
            {
                message = "Invalid CSV header.",
                expectedHeaders = requiredHeaders
            });

        var index = normalizedHeaders
            .Select((name, i) => new { name, i })
            .ToDictionary(x => x.name, x => x.i, StringComparer.OrdinalIgnoreCase);

        var errors = new List<object>();
        var rows = new List<BulkEmployeeRow>();
        var fileCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var lineNumber = 2; lineNumber <= lines.Count; lineNumber++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineNumber - 1])) continue;

            var values = ParseCsvLine(lines[lineNumber - 1]);
            string Get(string header) => index[header] < values.Count ? values[index[header]].Trim() : string.Empty;

            var row = new BulkEmployeeRow(
                lineNumber,
                Get("OrganizationCode"),
                NormalizeEmployeeCode(Get("EmployeeCode")),
                Get("FullName"),
                Get("BranchCode"),
                Get("DepartmentCode"),
                NormalizeEmployeeCode(Get("ReportingManagerCode")),
                Get("JoiningDate"),
                Get("IsActive"));

            var rowErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(row.EmployeeCode)) rowErrors.Add("EmployeeCode is required.");
            if (string.IsNullOrWhiteSpace(row.FullName)) rowErrors.Add("FullName is required.");
            if (!fileCodes.Add(row.EmployeeCode)) rowErrors.Add("Duplicate EmployeeCode in the file.");
            if (await db.Employees.AnyAsync(x => x.EmployeeCode == row.EmployeeCode, ct))
                rowErrors.Add("EmployeeCode already exists.");

            if (!DateOnly.TryParse(row.JoiningDate, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.None, out _)
                && !DateOnly.TryParseExact(row.JoiningDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                rowErrors.Add("JoiningDate must be a valid date, preferably yyyy-MM-dd.");

            if (!TryParseBoolean(row.IsActive, out _))
                rowErrors.Add("IsActive must be true/false, 1/0, yes/no.");

            if (rowErrors.Count > 0)
                errors.Add(new { row = lineNumber, employeeCode = row.EmployeeCode, errors = rowErrors });
            else
                rows.Add(row);
        }

        if (errors.Count > 0)
            return BadRequest(new
            {
                message = "Upload validation failed. No employees were imported.",
                totalRows = lines.Count - 1,
                errorRows = errors.Count,
                errors
            });

        var organizationCodes = rows.Select(x => x.OrganizationCode).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var organizations = await db.Organizations
            .Where(x => organizationCodes.Contains(x.OrganizationCode))
            .ToListAsync(ct);

        var branches = await db.Branches.AsNoTracking().ToListAsync(ct);
        var departments = await db.Departments.AsNoTracking().ToListAsync(ct);
        var existingEmployees = await db.Employees.AsNoTracking().ToListAsync(ct);

        var validationErrors = new List<object>();
        var parsedRows = new List<(BulkEmployeeRow Row, DateOnly JoiningDate, bool IsActive, Guid? BranchId, Guid? DepartmentId)>();

        foreach (var row in rows)
        {
            var rowErrors = new List<string>();
            var org = organizations.FirstOrDefault(x => x.OrganizationCode.Equals(row.OrganizationCode, StringComparison.OrdinalIgnoreCase) && x.IsActive);
            if (org is null)
                rowErrors.Add("OrganizationCode does not reference an active organization.");

            Guid? branchId = null;
            if (!string.IsNullOrWhiteSpace(row.BranchCode))
            {
                var branch = org is null ? null : branches.FirstOrDefault(x =>
                    x.OrganizationId == org.Id &&
                    x.BranchCode.Equals(row.BranchCode, StringComparison.OrdinalIgnoreCase) &&
                    x.IsActive);
                if (branch is null) rowErrors.Add("BranchCode does not reference an active branch for the organization.");
                else branchId = branch.Id;
            }

            Guid? departmentId = null;
            if (!string.IsNullOrWhiteSpace(row.DepartmentCode))
            {
                var department = org is null ? null : departments.FirstOrDefault(x =>
                    x.OrganizationId == org.Id &&
                    x.DepartmentCode.Equals(row.DepartmentCode, StringComparison.OrdinalIgnoreCase) &&
                    x.IsActive);
                if (department is null) rowErrors.Add("DepartmentCode does not reference an active department for the organization.");
                else departmentId = department.Id;
            }

            var joiningDate = DateOnly.TryParseExact(row.JoiningDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate)
                ? isoDate
                : DateOnly.Parse(row.JoiningDate, CultureInfo.GetCultureInfo("en-IN"));

            TryParseBoolean(row.IsActive, out var isActive);

            if (rowErrors.Count > 0)
                validationErrors.Add(new { row = row.LineNumber, employeeCode = row.EmployeeCode, errors = rowErrors });
            else
                parsedRows.Add((row, joiningDate, isActive, branchId, departmentId));
        }

        // Reporting managers may already exist in the database OR be another active employee
        // in the same CSV upload. Normalize codes so spaces/casing do not cause false failures.
        var managerCodes = parsedRows
            .Select(x => x.Row.ReportingManagerCode.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var managerMap = existingEmployees
            .Where(x => x.IsActive)
            .GroupBy(x => NormalizeEmployeeCode(x.EmployeeCode), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Id, StringComparer.OrdinalIgnoreCase);

        // Add employees from this upload as possible managers.
        // Only active rows can be referenced as reporting managers.
        foreach (var item in parsedRows.Where(x => x.IsActive))
        {
            var code = item.Row.EmployeeCode.Trim();
            if (!managerMap.ContainsKey(code))
                managerMap[code] = Guid.Empty; // resolved to the new entity Id after insert
        }

        foreach (var item in parsedRows)
        {
            var managerCode = item.Row.ReportingManagerCode.Trim();

            if (managerCode.Length == 0)
                continue;

            if (!managerMap.ContainsKey(managerCode))
            {
                validationErrors.Add(new
                {
                    row = item.Row.LineNumber,
                    employeeCode = item.Row.EmployeeCode,
                    errors = new[] { "ReportingManagerCode must reference an existing active employee or an active employee in this upload." }
                });
            }

            if (managerCode.Equals(item.Row.EmployeeCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                validationErrors.Add(new
                {
                    row = item.Row.LineNumber,
                    employeeCode = item.Row.EmployeeCode,
                    errors = new[] { "An employee cannot report to themselves." }
                });
            }
        }

        if (validationErrors.Count > 0)
            return BadRequest(new
            {
                message = "Upload validation failed. No employees were imported.",
                totalRows = lines.Count - 1,
                errorRows = validationErrors.Count,
                errors = validationErrors
            });

        // SQL Server retry-on-failure is enabled in Infrastructure. User-initiated
        // transactions must therefore run inside EF Core's execution strategy.
        var executionStrategy = db.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var entities = parsedRows.Select(x => new Employee
                {
                    Id = Guid.NewGuid(),
                    EmployeeCode = x.Row.EmployeeCode,
                    FullName = x.Row.FullName,
                    BranchId = x.BranchId,
                    DepartmentId = x.DepartmentId,
                    JoiningDate = x.JoiningDate,
                    IsActive = x.IsActive
                }).ToList();

                db.Employees.AddRange(entities);
                await db.SaveChangesAsync(ct);

                // Resolve manager codes to both existing employees and employees inserted by this upload.
                for (var i = 0; i < entities.Count; i++)
                {
                    var managerCode = parsedRows[i].Row.ReportingManagerCode.Trim();

                    if (managerCode.Length == 0)
                        continue;

                    var managerEntity = entities.FirstOrDefault(x =>
                        x.EmployeeCode.Trim().Equals(managerCode, StringComparison.OrdinalIgnoreCase));

                    if (managerEntity is not null)
                    {
                        entities[i].ReportingManagerId = managerEntity.Id;
                    }
                    else if (managerMap.TryGetValue(managerCode, out var existingManagerId))
                    {
                        entities[i].ReportingManagerId = existingManagerId;
                    }
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return Ok(new
                {
                    message = "Employee bulk upload completed.",
                    imported = entities.Count,
                    totalRows = lines.Count - 1
                });
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Employee input, CancellationToken ct)
    {
        var item = await db.Employees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.EmployeeCode) || string.IsNullOrWhiteSpace(input.FullName))
            return BadRequest("EmployeeCode and FullName are required.");
        if (await db.Employees.AnyAsync(x => x.Id != id && x.EmployeeCode == input.EmployeeCode, ct))
            return Conflict("EmployeeCode already exists.");
        var validation = await ValidateReferences(input, ct, id);
        if (validation is not null) return validation;
        item.EmployeeCode = input.EmployeeCode.Trim();
        item.FullName = input.FullName.Trim();
        item.DateOfBirth = input.DateOfBirth;
        item.Gender = string.IsNullOrWhiteSpace(input.Gender) ? null : input.Gender.Trim();
        item.MobileNumber = string.IsNullOrWhiteSpace(input.MobileNumber) ? null : input.MobileNumber.Trim();
        item.EmailAddress = string.IsNullOrWhiteSpace(input.EmailAddress) ? null : input.EmailAddress.Trim();
        item.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
        item.EmergencyContactName = string.IsNullOrWhiteSpace(input.EmergencyContactName) ? null : input.EmergencyContactName.Trim();
        item.EmergencyContactNumber = string.IsNullOrWhiteSpace(input.EmergencyContactNumber) ? null : input.EmergencyContactNumber.Trim();
        item.EmergencyContactRelation = string.IsNullOrWhiteSpace(input.EmergencyContactRelation) ? null : input.EmergencyContactRelation.Trim();
        item.GradeId = input.GradeId;
        item.DepartmentId = input.DepartmentId;
        item.BranchId = input.BranchId;
        item.ReportingManagerId = input.ReportingManagerId;
        item.Designation = string.IsNullOrWhiteSpace(input.Designation) ? null : input.Designation.Trim();
        item.EmploymentType = string.IsNullOrWhiteSpace(input.EmploymentType) ? null : input.EmploymentType.Trim();
        item.JoiningDate = input.JoiningDate;
        item.ConfirmationDate = input.ConfirmationDate;
        item.BiometricUserId = string.IsNullOrWhiteSpace(input.BiometricUserId) ? null : input.BiometricUserId.Trim();
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        return Ok(item);
    }

    private async Task<BadRequestObjectResult?> ValidateReferences(Employee input, CancellationToken ct, Guid? currentEmployeeId = null)
    {
        if (input.GradeId.HasValue && !await db.EmployeeGrades.AnyAsync(x => x.Id == input.GradeId.Value && x.IsActive, ct))
            return BadRequest("GradeId does not reference an active employee grade.");
        if (input.DepartmentId.HasValue &&
            !await db.Departments.AnyAsync(x => x.Id == input.DepartmentId.Value && x.IsActive, ct))
            return BadRequest("DepartmentId does not reference an active department.");

        if (input.BranchId.HasValue &&
            !await db.Branches.AnyAsync(x => x.Id == input.BranchId.Value && x.IsActive, ct))
            return BadRequest("BranchId does not reference an active branch.");

        if (input.ReportingManagerId.HasValue)
        {
            if (currentEmployeeId.HasValue && input.ReportingManagerId.Value == currentEmployeeId.Value)
                return BadRequest("An employee cannot report to themselves.");

            if (!await db.Employees.AnyAsync(x => x.Id == input.ReportingManagerId.Value && x.IsActive, ct))
                return BadRequest("ReportingManagerId does not reference an active employee.");
        }

        return null;
    }

    private static string NormalizeHeader(string value) =>
        value.Trim().Trim('\uFEFF').Trim();

    // Employee codes are identifiers, not numeric values. Excel may remove leading
    // zeroes when saving CSV, so numeric codes are normalized to the HRMS five-digit
    // format: 206 -> 00206, 116 -> 00116, 00206 -> 00206.
    private static string NormalizeEmployeeCode(string value)
    {
        value = value.Trim();
        if (value.Length == 0) return value;

        if (value.All(char.IsDigit) && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            return number.ToString("D5", CultureInfo.InvariantCulture);

        return value;
    }

    private static bool TryParseBoolean(string value, out bool result)
    {
        if (bool.TryParse(value, out result)) return true;
        if (value is "1" or "yes" or "YES" or "Yes") { result = true; return true; }
        if (value is "0" or "no" or "NO" or "No") { result = false; return true; }
        result = false;
        return false;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (ch == ',' && !quoted)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }

    private sealed record BulkEmployeeRow(
        int LineNumber,
        string OrganizationCode,
        string EmployeeCode,
        string FullName,
        string BranchCode,
        string DepartmentCode,
        string ReportingManagerCode,
        string JoiningDate,
        string IsActive);
}