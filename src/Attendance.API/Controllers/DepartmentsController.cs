using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetAll(
        [FromQuery] Guid? organizationId = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var query = db.Departments.AsNoTracking();
        if (organizationId.HasValue) query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (activeOnly) query = query.Where(x => x.IsActive);
        return Ok(await query.OrderBy(x => x.DepartmentCode).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Department>> Get(Guid id, CancellationToken ct)
    {
        var item = await db.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Department>> Create(Department input, CancellationToken ct)
    {
        if (input.Id == Guid.Empty) input.Id = Guid.NewGuid();
        if (!await db.Organizations.AnyAsync(x => x.Id == input.OrganizationId && x.IsActive, ct))
            return BadRequest("Active OrganizationId is required.");
        if (await db.Departments.AnyAsync(x => x.OrganizationId == input.OrganizationId && x.DepartmentCode == input.DepartmentCode, ct))
            return Conflict("DepartmentCode already exists for this organization.");
        db.Departments.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Department input, CancellationToken ct)
    {
        var item = await db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (!await db.Organizations.AnyAsync(x => x.Id == input.OrganizationId && x.IsActive, ct))
            return BadRequest("Active OrganizationId is required.");
        if (await db.Departments.AnyAsync(x => x.Id != id && x.OrganizationId == input.OrganizationId && x.DepartmentCode == input.DepartmentCode, ct))
            return Conflict("DepartmentCode already exists for this organization.");
        item.OrganizationId = input.OrganizationId;
        item.DepartmentCode = input.DepartmentCode;
        item.DepartmentName = input.DepartmentName;
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}