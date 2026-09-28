using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/organizations")]
public sealed class OrganizationsController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organization>>> GetAll(CancellationToken ct) =>
        Ok(await db.Organizations.AsNoTracking().OrderBy(x => x.OrganizationName).ToListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Organization>> Get(Guid id, CancellationToken ct)
    {
        var item = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Organization>> Create(Organization input, CancellationToken ct)
    {
        if (input.Id == Guid.Empty) input.Id = Guid.NewGuid();
        if (await db.Organizations.AnyAsync(x => x.OrganizationCode == input.OrganizationCode, ct))
            return Conflict("OrganizationCode already exists.");
        db.Organizations.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }
}