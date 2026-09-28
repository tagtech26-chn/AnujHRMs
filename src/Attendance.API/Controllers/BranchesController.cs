using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/branches")]
public sealed class BranchesController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Branch>>> GetAll(
        [FromQuery] Guid? organizationId = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var query = db.Branches.AsNoTracking();
        if (organizationId.HasValue) query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (activeOnly) query = query.Where(x => x.IsActive);
        return Ok(await query.OrderBy(x => x.BranchCode).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Branch>> Get(Guid id, CancellationToken ct)
    {
        var item = await db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Branch>> Create(Branch input, CancellationToken ct)
    {
        if (input.Id == Guid.Empty) input.Id = Guid.NewGuid();
        if (!await db.Organizations.AnyAsync(x => x.Id == input.OrganizationId && x.IsActive, ct))
            return BadRequest("Active OrganizationId is required.");
        if (await db.Branches.AnyAsync(x => x.OrganizationId == input.OrganizationId && x.BranchCode == input.BranchCode, ct))
            return Conflict("BranchCode already exists for this organization.");
        db.Branches.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Branch input, CancellationToken ct)
    {
        var item = await db.Branches.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (!await db.Organizations.AnyAsync(x => x.Id == input.OrganizationId && x.IsActive, ct))
            return BadRequest("Active OrganizationId is required.");
        if (await db.Branches.AnyAsync(x => x.Id != id && x.OrganizationId == input.OrganizationId && x.BranchCode == input.BranchCode, ct))
            return Conflict("BranchCode already exists for this organization.");
        item.OrganizationId = input.OrganizationId;
        item.BranchCode = input.BranchCode;
        item.BranchName = input.BranchName;
        item.Address = input.Address;
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}