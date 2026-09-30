using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/leave-policies")]
public sealed class LeavePoliciesController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeavePolicy>>> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var query = db.LeavePolicies.AsNoTracking();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return Ok(await query.OrderBy(x => x.PolicyName).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeavePolicy>> Get(Guid id, CancellationToken ct)
    {
        var item = await db.LeavePolicies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<LeavePolicy>> Create(LeavePolicy input, CancellationToken ct)
    {
        if (input.Id == Guid.Empty) input.Id = Guid.NewGuid();
        if (!await db.LeaveTypes.AnyAsync(x => x.Id == input.LeaveTypeId && x.IsActive, ct))
            return BadRequest("Active LeaveTypeId is required.");
        var validation = ValidatePolicy(input);
        if (validation is not null) return BadRequest(validation);
        input.CreatedAtUtc = DateTime.UtcNow;
        input.AccrualType = input.AccrualType.Trim();
        if (input.AccrualType == "Monthly") input.AnnualEntitlement = null;
        if (input.AccrualType == "None") input.MonthlyEntitlement = 0;
        db.LeavePolicies.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeavePolicy>> Update(Guid id, LeavePolicy input, CancellationToken ct)
    {
        var item = await db.LeavePolicies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (!await db.LeaveTypes.AnyAsync(x => x.Id == input.LeaveTypeId && x.IsActive, ct))
            return BadRequest("Active LeaveTypeId is required.");
        var validation = ValidatePolicy(input);
        if (validation is not null) return BadRequest(validation);
        item.LeaveTypeId = input.LeaveTypeId;
        item.PolicyName = input.PolicyName.Trim();
        item.AccrualType = input.AccrualType.Trim();
        item.MonthlyEntitlement = item.AccrualType == "Monthly" ? input.MonthlyEntitlement : 0;
        item.AnnualEntitlement = item.AccrualType == "Annual" ? input.AnnualEntitlement : null;
        item.CarryForwardAllowed = input.CarryForwardAllowed;
        item.MaximumCarryForward = input.CarryForwardAllowed ? input.MaximumCarryForward : 0;
        item.AllowNegativeBalance = input.AllowNegativeBalance;
        item.IsActive = input.IsActive;
        item.EffectiveFrom = input.EffectiveFrom;
        item.EffectiveTo = input.EffectiveTo;
        await db.SaveChangesAsync(ct);
        return Ok(item);
    }

    private static string? ValidatePolicy(LeavePolicy input)
    {
        if (string.IsNullOrWhiteSpace(input.PolicyName)) return "PolicyName is required.";
        if (!new[] { "Monthly", "Annual", "None" }.Contains(input.AccrualType, StringComparer.OrdinalIgnoreCase))
            return "AccrualType must be Monthly, Annual, or None.";
        if (input.MonthlyEntitlement < 0 || input.AnnualEntitlement < 0 || input.MaximumCarryForward < 0)
            return "Entitlements and carry-forward values cannot be negative.";
        if (input.AccrualType.Equals("Monthly", StringComparison.OrdinalIgnoreCase) && input.MonthlyEntitlement <= 0)
            return "MonthlyEntitlement must be greater than zero for monthly accrual.";
        if (input.AccrualType.Equals("Annual", StringComparison.OrdinalIgnoreCase) && (!input.AnnualEntitlement.HasValue || input.AnnualEntitlement.Value <= 0))
            return "AnnualEntitlement must be greater than zero for annual accrual.";
        if (input.EffectiveTo.HasValue && input.EffectiveTo.Value < input.EffectiveFrom)
            return "EffectiveTo cannot be before EffectiveFrom.";
        if (!input.CarryForwardAllowed && input.MaximumCarryForward != 0)
            return "MaximumCarryForward must be zero when carry-forward is disabled.";
        return null;
    }
}
