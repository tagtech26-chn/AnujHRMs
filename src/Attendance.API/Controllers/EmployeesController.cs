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
        if (await db.Employees.AnyAsync(x => x.EmployeeCode == input.EmployeeCode, ct))
            return Conflict("EmployeeCode already exists.");
        db.Employees.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Employee input, CancellationToken ct)
    {
        var item = await db.Employees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        item.EmployeeCode = input.EmployeeCode; item.FullName = input.FullName;
        item.DepartmentId = input.DepartmentId; item.BranchId = input.BranchId;
        item.ReportingManagerId = input.ReportingManagerId; item.JoiningDate = input.JoiningDate;
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}