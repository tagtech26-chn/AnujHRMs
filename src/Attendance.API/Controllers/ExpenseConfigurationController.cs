using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/expense/configuration")]
public sealed class ExpenseConfigurationController(AnujHrmsDbContext db) : ControllerBase
{
    private const string FinanceApproverEmployeeCodeKey = "Expense.FinanceApproverEmployeeCode";

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var setting = await db.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SettingKey == FinanceApproverEmployeeCodeKey && x.IsActive, ct);

        var code = setting?.SettingValue;
        var employee = string.IsNullOrWhiteSpace(code)
            ? null
            : await db.Employees.AsNoTracking()
                .Where(x => x.EmployeeCode == code && x.IsActive)
                .Select(x => new { x.Id, x.EmployeeCode, x.FullName })
                .FirstOrDefaultAsync(ct);

        return Ok(new
        {
            financeApproverEmployeeCode = code,
            financeApprover = employee,
            settingKey = FinanceApproverEmployeeCodeKey
        });
    }

    [HttpPut("finance-approver")]
    public async Task<IActionResult> SetFinanceApprover([FromBody] FinanceApproverInput input, CancellationToken ct)
    {
        var code = input.EmployeeCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("EmployeeCode is required.");

        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeCode == code && x.IsActive, ct);

        if (employee is null)
            return BadRequest("Finance approver must reference an active employee.");

        var setting = await db.SystemSettings
            .FirstOrDefaultAsync(x => x.SettingKey == FinanceApproverEmployeeCodeKey, ct);

        if (setting is null)
        {
            setting = new SystemSetting
            {
                Id = Guid.NewGuid(),
                SettingKey = FinanceApproverEmployeeCodeKey
            };
            db.SystemSettings.Add(setting);
        }

        setting.SettingValue = employee.EmployeeCode;
        setting.Description = "Employee code used for Finance approval in travel and expense workflows.";
        setting.IsActive = true;
        setting.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            settingKey = setting.SettingKey,
            financeApproverEmployeeCode = setting.SettingValue,
            financeApprover = new { employee.Id, employee.EmployeeCode, employee.FullName }
        });
    }

    public sealed record FinanceApproverInput(string? EmployeeCode);
}
