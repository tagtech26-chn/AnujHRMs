using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/leave-types")]
public sealed class LeaveTypesController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeaveType>>> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var query = db.LeaveTypes.AsNoTracking();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return Ok(await query.OrderBy(x => x.LeaveCode).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveType>> Get(Guid id, CancellationToken ct)
    {
        var item = await db.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<LeaveType>> Create(LeaveType input, CancellationToken ct)
    {
        if (input.Id == Guid.Empty) input.Id = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(input.LeaveCode) || string.IsNullOrWhiteSpace(input.LeaveName))
            return BadRequest("LeaveCode and LeaveName are required.");
        input.LeaveCode = input.LeaveCode.Trim().ToUpperInvariant();
        input.LeaveName = input.LeaveName.Trim();
        if (await db.LeaveTypes.AnyAsync(x => x.LeaveCode == input.LeaveCode, ct))
            return Conflict("LeaveCode already exists.");
        input.CreatedAtUtc = DateTime.UtcNow;
        db.LeaveTypes.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeaveType>> Update(Guid id, LeaveType input, CancellationToken ct)
    {
        var item = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        var code = input.LeaveCode.Trim().ToUpperInvariant();
        if (await db.LeaveTypes.AnyAsync(x => x.Id != id && x.LeaveCode == code, ct))
            return Conflict("LeaveCode already exists.");
        item.LeaveCode = code;
        item.LeaveName = input.LeaveName.Trim();
        item.Description = input.Description;
        item.IsPaid = input.IsPaid;
        item.IsHalfDayAllowed = input.IsHalfDayAllowed;
        item.RequiresAttachment = input.RequiresAttachment;
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        return Ok(item);
    }
}
