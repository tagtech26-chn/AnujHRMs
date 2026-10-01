using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Attendance.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendance.API.Controllers;

[ApiController]
[Route("api/expense")]
public sealed class ExpenseController(AnujHrmsDbContext db, ITravelExpensePolicyValidator validator) : ControllerBase
{
    [HttpGet("workflow/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> GetWorkflowHistory(string entityType, Guid entityId, CancellationToken ct)
    {
        var rows = await db.ExpenseWorkflowHistories.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderBy(x => x.ActionedAtUtc)
            .ToListAsync(ct);
        return Ok(rows);
    }

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
        db.TravelRequests.Add(input);
        await db.SaveChangesAsync(ct);
        await AddHistory("TravelRequest", input.Id, "Created", "", input.Status, input.EmployeeId, null, ct);
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
        var fromStatus = x.Status;
        x.Status="PendingManager"; x.SubmittedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AddHistory("TravelRequest", x.Id, "Submit", fromStatus, x.Status, x.EmployeeId, null, ct);
        return Ok(x);
    }

    [HttpPost("travel-requests/{id:guid}/manager-decision")]
    public async Task<IActionResult> TravelManagerDecision(Guid id, [FromBody] DecisionInput input, CancellationToken ct)
        => await TravelDecision(id, input, true, ct);

    [HttpPost("travel-requests/{id:guid}/finance-decision")]
    public async Task<IActionResult> TravelFinanceDecision(Guid id, [FromBody] DecisionInput input, CancellationToken ct)
        => await TravelDecision(id, input, false, ct);

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
        if (input.TravelRequestId.HasValue)
        {
            var request = await db.TravelRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.TravelRequestId.Value, ct);
            if (request is null || request.EmployeeId != input.EmployeeId) return BadRequest("Travel request is invalid.");
            if (request.Status != "Approved") return BadRequest("The linked travel request must be fully approved before creating an expense claim.");
            if (input.TravelFrom < request.TravelFrom || input.TravelTo > request.TravelTo) return BadRequest("Claim period must be within the approved travel request period.");
        }
        input.Id=Guid.NewGuid(); input.ClaimNumber=await NextNumber("EXP");
        input.ClaimDate=DateOnly.FromDateTime(DateTime.Now);
        input.TravelDuration=input.TravelFrom==input.TravelTo?"SingleDay":"MultiDay";
        input.Status="Draft"; input.ReportingManagerId=employee.ReportingManagerId;
        input.TotalClaimedAmount=0;input.TotalEligibleAmount=0;input.TotalRejectedAmount=0;input.CreatedAtUtc=DateTime.UtcNow;
        db.ExpenseClaims.Add(input);await db.SaveChangesAsync(ct);
        await AddHistory("ExpenseClaim", input.Id, "Created", "", input.Status, input.EmployeeId, null, ct);
        return CreatedAtAction(nameof(GetClaim),new{id=input.Id},input);
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
        var validation = await validator.ValidateLineAsync(claim.EmployeeId, claim, input, ct);
        input.EligibleAmount=validation.EligibleAmount; input.RejectedAmount=validation.RejectedAmount; input.ValidationStatus=validation.Status; input.ValidationMessage=validation.Message; input.RequiresAttachment=validation.RequiresAttachment;
        db.ExpenseClaimLines.Add(input);
        await Recalculate(claim,ct);
        await db.SaveChangesAsync(ct);
        return Ok(input);
    }

    [HttpPost("claims/{id:guid}/validate")]
    public async Task<IActionResult> ValidateClaim(Guid id,CancellationToken ct)
    {
        var claim=await db.ExpenseClaims.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(claim is null)return NotFound();
        var lines=await db.ExpenseClaimLines.Where(x=>x.ExpenseClaimId==id).ToListAsync(ct);
        if(lines.Count==0)return BadRequest("Add at least one expense line.");
        foreach(var line in lines){var v=await validator.ValidateLineAsync(claim.EmployeeId,claim,line,ct);line.EligibleAmount=v.EligibleAmount;line.RejectedAmount=v.RejectedAmount;line.ValidationStatus=v.Status;line.ValidationMessage=v.Message;line.RequiresAttachment=v.RequiresAttachment;}
        await Recalculate(claim,ct); await db.SaveChangesAsync(ct);
        return Ok(new{claim,lines});
    }

    [HttpPost("claims/{id:guid}/submit")]
    public async Task<IActionResult> SubmitClaim(Guid id,CancellationToken ct)
    {
        var claim=await db.ExpenseClaims.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(claim is null)return NotFound();
        if(claim.Status!="Draft")return BadRequest("Only draft claims can be submitted.");
        var lines=await db.ExpenseClaimLines.Where(x=>x.ExpenseClaimId==id).ToListAsync(ct);
        if(lines.Count==0)return BadRequest("Add at least one expense line.");
        foreach(var line in lines)
        {
            var validation=await validator.ValidateLineAsync(claim.EmployeeId,claim,line,ct);
            line.EligibleAmount=validation.EligibleAmount; line.RejectedAmount=validation.RejectedAmount; line.ValidationStatus=validation.Status; line.ValidationMessage=validation.Message; line.RequiresAttachment=validation.RequiresAttachment;
        }
        if(lines.Any(x=>x.RequiresAttachment&&!x.AttachmentProvided))return BadRequest("Required attachments are missing.");
        if(!claim.ReportingManagerId.HasValue)return BadRequest("Employee has no reporting manager configured.");
        await Recalculate(claim,ct);
        var fromStatus = claim.Status;
        claim.Status="PendingManager";claim.SubmittedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AddHistory("ExpenseClaim", claim.Id, "Submit", fromStatus, claim.Status, claim.EmployeeId, null, ct);
        return Ok(claim);
    }

    [HttpPost("claims/{id:guid}/manager-decision")]
    public async Task<IActionResult> ManagerDecision(Guid id,[FromBody] DecisionInput input,CancellationToken ct)
        => await Decision(id,input,true,ct);

    [HttpPost("claims/{id:guid}/finance-decision")]
    public async Task<IActionResult> FinanceDecision(Guid id,[FromBody] DecisionInput input,CancellationToken ct)
        => await Decision(id,input,false,ct);

    [HttpPost("claims/{id:guid}/settle")]
    public async Task<IActionResult> SettleClaim(Guid id, [FromBody] SettlementInput input, CancellationToken ct)
    {
        var claim = await db.ExpenseClaims.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (claim is null) return NotFound();
        if (claim.Status != "Approved") return BadRequest("Only approved claims can be settled.");
        var from = claim.Status;
        claim.Status = "Settled";
        claim.SettledAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await AddHistory("ExpenseClaim", claim.Id, "Settlement", from, claim.Status, input.ActorEmployeeId, input.Remarks, ct);
        return Ok(claim);
    }

    private async Task<IActionResult> Decision(Guid id,DecisionInput input,bool manager,CancellationToken ct)
    {
        var claim=await db.ExpenseClaims.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(claim is null)return NotFound();
        var expected=manager?"PendingManager":"PendingFinance";
        if(claim.Status!=expected)return BadRequest($"Claim must be {expected}.");
        var fromStatus = claim.Status;
        Guid? actorId = input.ApproverId;
        if(input.Approve)
        {
            if(manager)
            {
                if (claim.ReportingManagerId.HasValue && actorId.HasValue && actorId.Value != claim.ReportingManagerId.Value)
                    return BadRequest("Only the reporting manager can approve this claim.");
                claim.Status="PendingFinance";claim.ManagerRemarks=input.Remarks;actorId=claim.ReportingManagerId;
            }
            else
            {
                var finance=await db.Employees.AsNoTracking().FirstOrDefaultAsync(x=>x.EmployeeCode=="00097"&&x.IsActive,ct);
                if(finance is null)return BadRequest("Finance approver employee code 00097 is not configured.");
                if(input.ApproverId.HasValue&&input.ApproverId.Value!=finance.Id)return BadRequest("Only finance employee 00097 can approve claims.");
                claim.Status="Approved";claim.FinanceRemarks=input.Remarks;claim.FinanceApproverId=finance.Id;claim.ApprovedAtUtc=DateTime.UtcNow;actorId=finance.Id;
            }
        }
        else
        {
            claim.Status = "Rejected";
            if(manager) claim.ManagerRemarks=input.Remarks; else claim.FinanceRemarks=input.Remarks;
        }
        await db.SaveChangesAsync(ct);
        await AddHistory("ExpenseClaim", claim.Id, input.Approve ? (manager ? "ManagerApprove" : "FinanceApprove") : "Reject", fromStatus, claim.Status, actorId, input.Remarks, ct);
        return Ok(claim);
    }

    private async Task<IActionResult> TravelDecision(Guid id, DecisionInput input, bool manager, CancellationToken ct)
    {
        var request = await db.TravelRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (request is null) return NotFound();
        var expected = manager ? "PendingManager" : "PendingFinance";
        if (request.Status != expected) return BadRequest($"Travel request must be {expected}.");
        var fromStatus = request.Status;
        Guid? actorId = input.ApproverId;

        if (input.Approve)
        {
            if (manager)
            {
                if (request.ReportingManagerId.HasValue && actorId.HasValue && actorId.Value != request.ReportingManagerId.Value)
                    return BadRequest("Only the reporting manager can approve this travel request.");
                request.Status = "PendingFinance";
                request.ManagerRemarks = input.Remarks;
                actorId = request.ReportingManagerId;
            }
            else
            {
                var finance = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeCode == "00097" && x.IsActive, ct);
                if (finance is null) return BadRequest("Finance approver employee code 00097 is not configured.");
                if (input.ApproverId.HasValue && input.ApproverId.Value != finance.Id)
                    return BadRequest("Only finance employee 00097 can approve travel requests.");
                request.Status = "Approved";
                request.FinanceRemarks = input.Remarks;
                request.FinanceApproverId = finance.Id;
                request.ApprovedAtUtc = DateTime.UtcNow;
                actorId = finance.Id;
            }
        }
        else
        {
            request.Status = "Rejected";
            if (manager) request.ManagerRemarks = input.Remarks; else request.FinanceRemarks = input.Remarks;
        }

        await db.SaveChangesAsync(ct);
        await AddHistory("TravelRequest", request.Id, input.Approve ? (manager ? "ManagerApprove" : "FinanceApprove") : "Reject",
            fromStatus, request.Status, actorId, input.Remarks, ct);
        return Ok(request);
    }

    private async Task Recalculate(ExpenseClaim claim,CancellationToken ct)
    {
        var lines=await db.ExpenseClaimLines.Where(x=>x.ExpenseClaimId==claim.Id).ToListAsync(ct);
        claim.TotalClaimedAmount=lines.Sum(x=>x.ClaimedAmount);
        claim.TotalEligibleAmount=lines.Sum(x=>x.EligibleAmount);
        claim.TotalRejectedAmount=lines.Sum(x=>x.RejectedAmount);
    }

    private async Task AddHistory(string entityType, Guid entityId, string action, string fromStatus, string toStatus, Guid? actorEmployeeId, string? remarks, CancellationToken ct)
    {
        db.ExpenseWorkflowHistories.Add(new ExpenseWorkflowHistory
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorEmployeeId = actorEmployeeId,
            Remarks = remarks,
            ActionedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
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
    public sealed record SettlementInput(Guid? ActorEmployeeId,string? Remarks);
}
