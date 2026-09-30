using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/travel-policies")]
public sealed class TravelPoliciesController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPolicies(CancellationToken ct)
    {
        var items = await db.TravelPolicies.AsNoTracking().OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPolicy(Guid id, CancellationToken ct)
    {
        var policy = await db.TravelPolicies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (policy is null) return NotFound();
        var rules = await db.TravelPolicyRules.AsNoTracking().Where(x => x.TravelPolicyId == id).OrderBy(x => x.RuleType).ThenBy(x => x.EmployeeGradeId).ToListAsync(ct);
        return Ok(new { policy, rules });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolicy(TravelPolicy input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.PolicyCode) || string.IsNullOrWhiteSpace(input.PolicyName))
            return BadRequest("PolicyCode and PolicyName are required.");
        input.Id = input.Id == Guid.Empty ? Guid.NewGuid() : input.Id;
        input.PolicyCode = input.PolicyCode.Trim().ToUpperInvariant();
        input.PolicyName = input.PolicyName.Trim();
        input.CreatedAtUtc = DateTime.UtcNow;
        db.TravelPolicies.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetPolicy), new { id = input.Id }, input);
    }

    [HttpPost("{policyId:guid}/rules")]
    public async Task<IActionResult> AddRule(Guid policyId, TravelPolicyRule input, CancellationToken ct)
    {
        if (!await db.TravelPolicies.AnyAsync(x => x.Id == policyId, ct)) return NotFound("Travel policy not found.");
        if (input.EmployeeGradeId.HasValue && !await db.EmployeeGrades.AnyAsync(x => x.Id == input.EmployeeGradeId && x.IsActive, ct))
            return BadRequest("EmployeeGradeId does not reference an active grade.");
        input.Id = input.Id == Guid.Empty ? Guid.NewGuid() : input.Id;
        input.TravelPolicyId = policyId;
        input.RuleType = input.RuleType.Trim();
        db.TravelPolicyRules.Add(input);
        await db.SaveChangesAsync(ct);
        return Ok(input);
    }

    [HttpGet("{policyId:guid}/exceptions")]
    public async Task<IActionResult> GetExceptions(Guid policyId, CancellationToken ct)
    {
        var items = await db.TravelPolicyExceptions.AsNoTracking()
            .Where(x => x.IsActive && (x.EffectiveTo == null || x.EffectiveTo >= DateOnly.FromDateTime(DateTime.UtcNow)) && x.EffectiveFrom <= DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderBy(x => x.ExceptionName).ToListAsync(ct);
        var ids = items.Select(x => x.Id).ToArray();
        var rules = await db.TravelPolicyExceptionRules.AsNoTracking().Where(x => ids.Contains(x.TravelPolicyExceptionId)).ToListAsync(ct);
        return Ok(items.Select(x => new { exception = x, rules = rules.Where(r => r.TravelPolicyExceptionId == x.Id) }));
    }

    [HttpPost("{policyId:guid}/exceptions")]
    public async Task<IActionResult> AddException(Guid policyId, TravelPolicyException input, CancellationToken ct)
    {
        if (!await db.TravelPolicies.AnyAsync(x => x.Id == policyId, ct)) return NotFound("Travel policy not found.");
        if (input.EmployeeId.HasValue && !await db.Employees.AnyAsync(x => x.Id == input.EmployeeId && x.IsActive, ct))
            return BadRequest("EmployeeId does not reference an active employee.");
        if (input.DepartmentId.HasValue && !await db.Departments.AnyAsync(x => x.Id == input.DepartmentId && x.IsActive, ct))
            return BadRequest("DepartmentId does not reference an active department.");
        if (input.EmployeeGradeId.HasValue && !await db.EmployeeGrades.AnyAsync(x => x.Id == input.EmployeeGradeId && x.IsActive, ct))
            return BadRequest("EmployeeGradeId does not reference an active grade.");
        if (string.IsNullOrWhiteSpace(input.ExceptionName)) return BadRequest("ExceptionName is required.");
        input.Id = input.Id == Guid.Empty ? Guid.NewGuid() : input.Id;
        input.ExceptionName = input.ExceptionName.Trim();
        input.CreatedAtUtc = DateTime.UtcNow;
        db.TravelPolicyExceptions.Add(input);
        await db.SaveChangesAsync(ct);
        return Ok(input);
    }

    [HttpPost("exceptions/{exceptionId:guid}/rules")]
    public async Task<IActionResult> AddExceptionRule(Guid exceptionId, TravelPolicyExceptionRule input, CancellationToken ct)
    {
        if (!await db.TravelPolicyExceptions.AnyAsync(x => x.Id == exceptionId, ct)) return NotFound("Exception not found.");
        input.Id = input.Id == Guid.Empty ? Guid.NewGuid() : input.Id;
        input.TravelPolicyExceptionId = exceptionId;
        input.RuleType = input.RuleType.Trim();
        db.TravelPolicyExceptionRules.Add(input);
        await db.SaveChangesAsync(ct);
        return Ok(input);
    }

    [HttpGet("resolve/{employeeId:guid}")]
    public async Task<IActionResult> Resolve(Guid employeeId, [FromQuery] DateOnly? date, [FromQuery] string travelDuration = "All", [FromQuery] string? travelMode = null, [FromQuery] string? vehicleType = null, CancellationToken ct = default)
    {
        var effectiveDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId, ct);
        if (employee is null) return NotFound("Employee not found.");
        if (!employee.GradeId.HasValue) return BadRequest("Employee has no grade assigned.");

        var policy = await db.TravelPolicies.AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= effectiveDate && (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
        if (policy is null) return NotFound("No active travel policy is effective for the requested date.");

        var standard = await db.TravelPolicyRules.AsNoTracking()
            .Where(x => x.TravelPolicyId == policy.Id && x.IsActive && x.EmployeeGradeId == employee.GradeId)
            .ToListAsync(ct);
        standard = standard.Where(x => x.TravelDuration.Equals("All", StringComparison.OrdinalIgnoreCase) || x.TravelDuration.Equals(travelDuration, StringComparison.OrdinalIgnoreCase)).ToList();

        var exceptions = await db.TravelPolicyExceptions.AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= effectiveDate && (x.EffectiveTo == null || x.EffectiveTo >= effectiveDate) &&
                (x.EmployeeId == employee.Id || x.DepartmentId == employee.DepartmentId || x.EmployeeGradeId == employee.GradeId))
            .OrderByDescending(x => x.EmployeeId == employee.Id)
            .ThenByDescending(x => x.DepartmentId == employee.DepartmentId)
            .ThenByDescending(x => x.EmployeeGradeId == employee.GradeId)
            .ToListAsync(ct);

        var exceptionIds = exceptions.Select(x => x.Id).ToArray();
        var exceptionRules = await db.TravelPolicyExceptionRules.AsNoTracking()
            .Where(x => exceptionIds.Contains(x.TravelPolicyExceptionId) && x.IsActive)
            .ToListAsync(ct);
        exceptionRules = exceptionRules.Where(x => x.TravelDuration.Equals("All", StringComparison.OrdinalIgnoreCase) || x.TravelDuration.Equals(travelDuration, StringComparison.OrdinalIgnoreCase)).ToList();

        IEnumerable<object> Filter(IEnumerable<TravelPolicyRule> rules) => rules.Where(x =>
            (string.IsNullOrWhiteSpace(travelMode) || string.IsNullOrWhiteSpace(x.TravelMode) || x.TravelMode.Equals(travelMode, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(vehicleType) || string.IsNullOrWhiteSpace(x.VehicleType) || x.VehicleType.Equals(vehicleType, StringComparison.OrdinalIgnoreCase)));

        IEnumerable<object> FilterExceptions(IEnumerable<TravelPolicyExceptionRule> rules) => rules.Where(x =>
            (string.IsNullOrWhiteSpace(travelMode) || string.IsNullOrWhiteSpace(x.TravelMode) || x.TravelMode.Equals(travelMode, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(vehicleType) || string.IsNullOrWhiteSpace(x.VehicleType) || x.VehicleType.Equals(vehicleType, StringComparison.OrdinalIgnoreCase)));

        return Ok(new
        {
            effectiveDate,
            employee = new { employee.Id, employee.EmployeeCode, employee.FullName, employee.GradeId, employee.DepartmentId },
            policy = new { policy.Id, policy.PolicyCode, policy.PolicyName, policy.PolicyVersion, policy.EffectiveFrom, policy.EffectiveTo },
            standardRules = Filter(standard),
            exceptions = exceptions.Select(e => new
            {
                e.Id, e.ExceptionName, e.Reason, e.EmployeeId, e.DepartmentId, e.EmployeeGradeId,
                rules = FilterExceptions(exceptionRules.Where(r => r.TravelPolicyExceptionId == e.Id))
            })
        });
    }
}
