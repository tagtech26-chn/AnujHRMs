using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/employee-grades")]
public sealed class EmployeeGradesController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeGrade>>> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var q = db.EmployeeGrades.AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        return Ok(await q.OrderBy(x => x.GradeCode).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeGrade>> Get(Guid id, CancellationToken ct)
    {
        var item = await db.EmployeeGrades.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeGrade>> Create(EmployeeGrade input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.GradeCode) || string.IsNullOrWhiteSpace(input.GradeName))
            return BadRequest("GradeCode and GradeName are required.");
        input.Id = input.Id == Guid.Empty ? Guid.NewGuid() : input.Id;
        input.GradeCode = input.GradeCode.Trim().ToUpperInvariant();
        input.GradeName = input.GradeName.Trim();
        if (await db.EmployeeGrades.AnyAsync(x => x.GradeCode == input.GradeCode, ct))
            return Conflict("GradeCode already exists.");
        db.EmployeeGrades.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeGrade>> Update(Guid id, EmployeeGrade input, CancellationToken ct)
    {
        var item = await db.EmployeeGrades.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.GradeCode) || string.IsNullOrWhiteSpace(input.GradeName))
            return BadRequest("GradeCode and GradeName are required.");
        var code = input.GradeCode.Trim().ToUpperInvariant();
        if (await db.EmployeeGrades.AnyAsync(x => x.Id != id && x.GradeCode == code, ct))
            return Conflict("GradeCode already exists.");
        item.GradeCode = code;
        item.GradeName = input.GradeName.Trim();
        item.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        return Ok(item);
    }
}
