using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/expense")]
public sealed class ExpenseController(AnujHrmsDbContext db) : ControllerBase
{
    [HttpGet("travel-requests")]
    public async Task<IActionResult> GetTravelRequests([FromQuery] Guid? employeeId, CancellationToken ct)
    {
        var q=db.TravelRequests.AsNoTracking();
        if(employeeId.HasValue) q=q.Where(x=>x.EmployeeId==employeeId.Value);
        return Ok(await q.OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct));
    }

    [HttpPost("travel-requests")]
    public async Task<IActionResult> CreateTravelRequest(TravelRequest input, CancellationToken ct)
    {
        var employee=await db.Employees.FirstOrDefaultAsync(x=>x.Id==input.EmployeeId && x.IsActive,ct);
        if(employee is null) return BadRequest("Employee not found or inactive.");
        if(input.TravelTo<input.TravelFrom) return BadRequest("TravelTo cannot be before TravelFrom.");
        input.Id=Guid.NewGuid();
        input.RequestNumber=await NextNumber("TRV");
        input.TravelDuration=input.TravelFrom==input.TravelTo?"SingleDay":"MultiDay";
        input.Status="Draft";
        input.ReportingManagerId=employee.ReportingManagerId;
        input.CreatedAtUtc=DateTime.UtcNow;
        db.TravelRequests.Add(input); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetTravelRequest),new{id=input.Id},input);
    }

    [HttpGet("travel-requests/{id:guid}")]
    public async Task<IActionResult> GetTravelRequest(Guid id,CancellationToken ct)
    {
        var x=await db.TravelRequests.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id,ct);
        return x is null?NotFound():Ok(x);
    }

    [HttpPost("travel-requests/{id:guid}/submit")]
    public async Task<IActionResult> SubmitTravelRequest(Guid id,CancellationToken ct)
    {
        var x=await db.TravelRequests.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(x is null)return NotFound();
        if(x.Status!="Draft")return BadRequest("Only draft travel requests can be submitted.");
        if(!x.ReportingManagerId.HasValue)return BadRequest("Employee has no reporting manager configured.");
        x.Status="PendingManager"; x.SubmittedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return Ok(x);
    }

    [HttpGet("claims")]
    public async Task<IActionResult> GetClaims([FromQuery] Guid? employeeId,CancellationToken ct)
    {
        var q=db.ExpenseClaims.AsNoTracking();
        if(employeeId.HasValue)q=q.Where(x=>x.EmployeeId==employeeId.Value);
        return Ok(await q.OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct));
    }

    [HttpPost("claims")]
    public async Task<IActionResult> CreateClaim(ExpenseClaim input,CancellationToken ct)
    {
        var employee=await db.Employees.FirstOrDefaultAsync(x=>x.Id==input.EmployeeId&&x.IsActive,ct);
        if(employee is null)return BadRequest("Employee not found or inactive.");
        if(input.TravelTo<input.TravelFrom)return BadRequest("TravelTo cannot be before TravelFrom.");
        input.Id=Guid.NewGuid(); input.ClaimNumber=await NextNumber("EXP");
        input.ClaimDate=DateOnly.FromDateTime(DateTime.Now);
        input.TravelDuration=input.TravelFrom==input.TravelTo?"SingleDay":"MultiDay";
        input.Status="Draft"; input.ReportingManagerId=employee.ReportingManagerId;
        input.TotalClaimedAmount=0;input.TotalEligibleAmount=0;input.TotalRejectedAmount=0;input.CreatedAtUtc=DateTime.UtcNow;
        db.ExpenseClaims.Add(input);await db.SaveChangesAsync(ct);return CreatedAtAction(nameof(GetClaim),new{id=input.Id},input);
    }

    [HttpGet("claims/{id:guid}")]
    public async Task<IActionResult> GetClaim(Guid id,CancellationToken ct)
    {
        var claim=await db.ExpenseClaims.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(claim is null)return NotFound();
        var lines=await db.ExpenseClaimLines.AsNoTracking().Where(x=>x.ExpenseClaimId==id).ToListAsync(ct);
        return Ok(new{claim,lines});
    }

    [HttpPost("claims/{claimId:guid}/lines")]
    public async Task<IActionResult> AddLine(Guid claimId,ExpenseClaimLine input,CancellationToken ct)
    {
        var claim=await db.ExpenseClaims.FirstOrDefaultAsync(x=>x.Id==claimId,ct);
        if(claim is null)return NotFound("Claim not found.");
        if(claim.Status!="Draft")return BadRequest("Only draft claims can be edited.");
        if(input.ExpenseDate<claim.TravelFrom||input.ExpenseDate>claim.TravelTo)return BadRequest("Expense date is outside the travel period.");
        input.Id=Guid.NewGuid();input.ExpenseClaimId=claimId;
        input.EligibleAmount=input.ClaimedAmount;input.RejectedAmount=0;input.ValidationStatus="Pending";
        db.ExpenseClaimLines.Add(input);
        await Recalculate(claim,ct);
        await db.SaveChangesAsync(ct);
        return Ok(input);
    }

    [HttpPost("claims/{id:guid}/submit")]
    public async Task<IActionResult> SubmitClaim(Guid id,CancellationToken ct)
    {
        var claim=await db.ExpenseClaims.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(claim is null)return NotFound();
        if(claim.Status!="Draft")return BadRequest("Only draft claims can be submitted.");
        var lines=await db.ExpenseClaimLines.Where(x=>x.ExpenseClaimId==id).ToListAsync(ct);
        if(lines.Count==0)return BadRequest("Add at least one expense line.");
        if(lines.Any(x=>x.RequiresAttachment&&!x.AttachmentProvided))return BadRequest("Required attachments are missing.");
        if(!claim.ReportingManagerId.HasValue)return BadRequest("Employee has no reporting manager configured.");
        await Recalculate(claim,ct);
        claim.Status="PendingManager";claim.SubmittedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync(ct);return Ok(claim);
    }

    [HttpPost("claims/{id:guid}/manager-decision")]
    public async Task<IActionResult> ManagerDecision(Guid id,[FromBody] DecisionInput input,CancellationToken ct)
        => await Decision(id,input,true,ct);

    [HttpPost("claims/{id:guid}/finance-decision")]
    public async Task<IActionResult> FinanceDecision(Guid id,[FromBody] DecisionInput input,CancellationToken ct)
        => await Decision(id,input,false,ct);

    private async Task<IActionResult> Decision(Guid id,DecisionInput input,bool manager,CancellationToken ct)
    {
        var claim=await db.ExpenseClaims.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(claim is null)return NotFound();
        var expected=manager?"PendingManager":"PendingFinance";
        if(claim.Status!=expected)return BadRequest($"Claim must be {expected}.");
        if(input.Approve)
        {
            if(manager){claim.Status="PendingFinance";claim.ManagerRemarks=input.Remarks;}
            else{claim.Status="Approved";claim.FinanceRemarks=input.Remarks;claim.FinanceApproverId=input.ApproverId;claim.ApprovedAtUtc=DateTime.UtcNow;}
        }
        else {claim.Status="Rejected";if(manager)claim.ManagerRemarks=input.Remarks;else claim.FinanceRemarks=input.Remarks;}
        await db.SaveChangesAsync(ct);return Ok(claim);
    }

    private async Task Recalculate(ExpenseClaim claim,CancellationToken ct)
    {
        var lines=await db.ExpenseClaimLines.Where(x=>x.ExpenseClaimId==claim.Id).ToListAsync(ct);
        foreach(var line in lines)
        {
            line.EligibleAmount=line.ClaimedAmount;
            line.RejectedAmount=0;
            line.ValidationStatus="Pending";
            line.ValidationMessage=null;
        }
        claim.TotalClaimedAmount=lines.Sum(x=>x.ClaimedAmount);
        claim.TotalEligibleAmount=lines.Sum(x=>x.EligibleAmount);
        claim.TotalRejectedAmount=lines.Sum(x=>x.RejectedAmount);
    }

    private async Task<string> NextNumber(string prefix)
    {
        var today=DateTime.Now.ToString("yyyyMMdd");
        var count=prefix=="TRV"
            ?await db.TravelRequests.CountAsync(x=>x.CreatedAtUtc.Date==DateTime.UtcNow.Date)
            :await db.ExpenseClaims.CountAsync(x=>x.CreatedAtUtc.Date==DateTime.UtcNow.Date);
        return $"{prefix}-{today}-{count+1:0000}";
    }

    public sealed record DecisionInput(bool Approve,string? Remarks,Guid? ApproverId);
}
