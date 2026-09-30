using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/leave-balances")]
public sealed class LeaveBalancesController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet("employee/{employeeId:guid}")]
    public async Task<IActionResult> GetEmployeeBalances(Guid employeeId, [FromQuery] int? year = null, CancellationToken ct = default)
    {
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId, ct);
        if (employee is null) return NotFound("Employee not found.");

        var targetYear = year ?? DateTime.Today.Year;
        var rows = await db.EmployeeLeaveBalances.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.BalanceYear == targetYear)
            .Join(db.LeavePolicies.AsNoTracking(), b => b.LeavePolicyId, p => p.Id, (b,p) => new { b,p })
            .Join(db.LeaveTypes.AsNoTracking(), x => x.p.LeaveTypeId, t => t.Id, (x,t) => new
            {
                x.b.Id, x.b.EmployeeId, x.b.LeavePolicyId, LeaveTypeId=t.Id, LeaveCode=t.LeaveCode, LeaveName=t.LeaveName,
                x.p.PolicyName, x.p.AccrualType, x.p.MonthlyEntitlement, x.p.AnnualEntitlement,
                x.b.BalanceYear, x.b.BalanceMonth, x.b.EntitledDays, x.b.AdjustmentDays, x.b.UsedDays, x.b.ExpiredDays,
                AvailableDays=x.b.EntitledDays+x.b.AdjustmentDays-x.b.UsedDays-x.b.ExpiredDays
            })
            .OrderBy(x => x.BalanceMonth).ThenBy(x => x.LeaveCode)
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("accrue")]
    public async Task<IActionResult> Accrue([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (month is < 1 or > 12) return BadRequest("Month must be between 1 and 12.");
        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var employees = await db.Employees.Where(x => x.IsActive && x.JoiningDate <= lastDay).ToListAsync(ct);
        var policies = await db.LeavePolicies.Include(x => db.LeaveTypes).Where(x => x.IsActive && x.EffectiveFrom <= lastDay && (!x.EffectiveTo.HasValue || x.EffectiveTo >= firstDay)).ToListAsync(ct);
        var existing = await db.EmployeeLeaveBalances.Where(x => x.BalanceYear == year && x.BalanceMonth == month).ToListAsync(ct);
        var created = 0;

        foreach (var employee in employees)
        foreach (var policy in policies)
        {
            if (existing.Any(x => x.EmployeeId == employee.Id && x.LeavePolicyId == policy.Id)) continue;
            decimal entitlement = 0;
            if (policy.AccrualType.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
            {
                entitlement = policy.MonthlyEntitlement;
                if (employee.JoiningDate > firstDay)
                {
                    var daysInMonth = lastDay.Day;
                    var eligibleDays = lastDay.Day - employee.JoiningDate.Day + 1;
                    entitlement = Math.Round(policy.MonthlyEntitlement * eligibleDays / daysInMonth, 2, MidpointRounding.AwayFromZero);
                }
            }
            else if (policy.AccrualType.Equals("Annual", StringComparison.OrdinalIgnoreCase) && month == 1)
                entitlement = policy.AnnualEntitlement ?? 0;

            var balance = new EmployeeLeaveBalance
            {
                Id=Guid.NewGuid(), EmployeeId=employee.Id, LeavePolicyId=policy.Id,
                BalanceYear=year, BalanceMonth=month, EntitledDays=entitlement,
                CreatedAtUtc=DateTime.UtcNow, UpdatedAtUtc=DateTime.UtcNow
            };
            db.EmployeeLeaveBalances.Add(balance);
            if (entitlement > 0)
                db.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
                {
                    Id=Guid.NewGuid(), EmployeeLeaveBalanceId=balance.Id, TransactionType="Accrual",
                    TransactionDays=entitlement, TransactionDate=firstDay,
                    Remarks=$"Monthly accrual for {firstDay:yyyy-MM}", CreatedAtUtc=DateTime.UtcNow
                });
            created++;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { year, month, created, message = "Leave balances accrued. Existing monthly balances were left unchanged." });
    }
}
