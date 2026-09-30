using Attendance.Domain.Entities;
using Attendance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Services;

public sealed record ExpenseValidationResult(
    bool IsValid,
    decimal EligibleAmount,
    decimal RejectedAmount,
    bool RequiresAttachment,
    string Status,
    string Message,
    Guid? PolicyRuleId,
    Guid? ExceptionRuleId,
    string? RuleType);

public interface ITravelExpensePolicyValidator
{
    Task<ExpenseValidationResult> ValidateLineAsync(Guid employeeId, ExpenseClaim claim, ExpenseClaimLine line, CancellationToken ct);
}

public sealed class TravelExpensePolicyValidator(AnujHrmsDbContext db) : ITravelExpensePolicyValidator
{
    public async Task<ExpenseValidationResult> ValidateLineAsync(Guid employeeId, ExpenseClaim claim, ExpenseClaimLine line, CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId && x.IsActive, ct);
        if (employee is null) return Invalid("Employee not found or inactive.");
        if (!employee.GradeId.HasValue) return Invalid("Employee has no grade assigned.");

        var date = line.ExpenseDate;
        var policy = await db.TravelPolicies.AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
        if (policy is null) return Invalid("No active travel policy is effective for the expense date.");

        var standard = await db.TravelPolicyRules.AsNoTracking()
            .Where(x => x.TravelPolicyId == policy.Id && x.IsActive && x.EmployeeGradeId == employee.GradeId)
            .ToListAsync(ct);

        var exceptions = await db.TravelPolicyExceptions.AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date) &&
                (x.EmployeeId == employee.Id || x.DepartmentId == employee.DepartmentId || x.EmployeeGradeId == employee.GradeId))
            .OrderByDescending(x => x.EmployeeId == employee.Id)
            .ThenByDescending(x => x.DepartmentId == employee.DepartmentId)
            .ThenByDescending(x => x.EmployeeGradeId == employee.GradeId)
            .ToListAsync(ct);

        var exceptionIds = exceptions.Select(x => x.Id).ToArray();
        var exceptionRules = await db.TravelPolicyExceptionRules.AsNoTracking()
            .Where(x => exceptionIds.Contains(x.TravelPolicyExceptionId) && x.IsActive).ToListAsync(ct);

        var matchingStandard = standard.Where(x => Matches(x, claim, line)).ToList();
        var matchingException = exceptionRules.Where(x => MatchesException(x, claim, line)).ToList();

        var chosenExceptions = matchingException
            .GroupBy(x => x.RuleType, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => Scope(exceptions.First(e => e.Id == x.TravelPolicyExceptionId), employee)).First())
            .ToList();

        var exception = chosenExceptions.FirstOrDefault(x => SameType(x.RuleType, line.ExpenseType));
        var chosen = exception is null ? matchingStandard.FirstOrDefault(x => SameType(x.RuleType, line.ExpenseType)) : null;

        var rate = exception?.RatePerKm ?? chosen?.RatePerKm;
        var amount = exception?.Amount ?? chosen?.Amount;
        var maxKm = exception?.MaxKmPerDay ?? chosen?.MaxKmPerDay;
        var calculation = exception?.CalculationType ?? chosen?.CalculationType;
        var requiresAttachment = (exception?.RequiresAttachment ?? chosen?.RequiresAttachment ?? false) ||
                                  matchingStandard.Any(x => x.RequiresAttachment);

        if (string.Equals(line.ExpenseType, "AttachmentRequired", StringComparison.OrdinalIgnoreCase))
            requiresAttachment = true;

        decimal eligible;
        string message;

        if (rate.HasValue)
        {
            if (!line.DistanceKm.HasValue) return Invalid("DistanceKm is required for a per-kilometre expense.", requiresAttachment, chosen?.Id, exception?.Id, line.ExpenseType);
            var allowedKm = maxKm.HasValue ? Math.Min(line.DistanceKm.Value, maxKm.Value) : line.DistanceKm.Value;
            eligible = allowedKm * rate.Value;
            message = maxKm.HasValue && line.DistanceKm.Value > maxKm.Value
                ? $"Distance capped at {maxKm.Value:0.##} km/day by policy."
                : $"Eligible at {rate.Value:0.##} per km.";
        }
        else if (amount.HasValue && string.Equals(calculation, "PerDay", StringComparison.OrdinalIgnoreCase))
        {
            eligible = Math.Min(line.ClaimedAmount, amount.Value);
            message = $"Eligible up to {amount.Value:0.##} per day.";
        }
        else if (maxKm.HasValue && line.DistanceKm.HasValue)
        {
            if (line.DistanceKm.Value <= maxKm.Value)
            {
                eligible = line.ClaimedAmount;
                message = $"Within {maxKm.Value:0.##} km/day limit.";
            }
            else
            {
                return new ExpenseValidationResult(false, 0, line.ClaimedAmount, requiresAttachment, "Rejected",
                    $"Distance exceeds policy limit of {maxKm.Value:0.##} km/day.", chosen?.Id, exception?.Id, line.ExpenseType);
            }
        }
        else
        {
            eligible = line.ClaimedAmount;
            message = "No monetary cap is configured for this expense type in the effective policy; claimed amount retained for approval.";
        }

        if (eligible < 0) eligible = 0;
        var rejected = Math.Max(0, line.ClaimedAmount - eligible);
        return new ExpenseValidationResult(rejected == 0, eligible, rejected, requiresAttachment,
            rejected == 0 ? "Valid" : "PartiallyAllowed", message, chosen?.Id, exception?.Id, line.ExpenseType);
    }

    private static bool SameType(string? a, string? b) => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && a.Equals(b, StringComparison.OrdinalIgnoreCase);

    private static int Scope(TravelPolicyException e, Employee employee)
        => e.EmployeeId == employee.Id ? 3 : e.DepartmentId == employee.DepartmentId ? 2 : e.EmployeeGradeId == employee.GradeId ? 1 : 0;

    private static bool Matches(TravelPolicyRule r, ExpenseClaim claim, ExpenseClaimLine line)
        => DurationMatches(r.TravelDuration, claim.TravelDuration) &&
           ModeMatches(r.TravelMode, line.TravelMode) &&
           VehicleMatches(r.VehicleType, line.VehicleType);

    private static bool MatchesException(TravelPolicyExceptionRule r, ExpenseClaim claim, ExpenseClaimLine line)
        => DurationMatches(r.TravelDuration, claim.TravelDuration) &&
           ModeMatches(r.TravelMode, line.TravelMode) &&
           VehicleMatches(r.VehicleType, line.VehicleType);

    private static bool DurationMatches(string? rule, string? actual)
        => string.IsNullOrWhiteSpace(rule) || rule.Equals("All", StringComparison.OrdinalIgnoreCase) || rule.Equals(actual, StringComparison.OrdinalIgnoreCase);

    private static bool ModeMatches(string? rule, string? actual)
        => string.IsNullOrWhiteSpace(rule) || string.IsNullOrWhiteSpace(actual) || rule.Equals(actual, StringComparison.OrdinalIgnoreCase);

    private static bool VehicleMatches(string? rule, string? actual)
        => string.IsNullOrWhiteSpace(rule) || string.IsNullOrWhiteSpace(actual) || rule.Equals(actual, StringComparison.OrdinalIgnoreCase);

    private static ExpenseValidationResult Invalid(string message, bool attachment = false, Guid? policyRuleId = null, Guid? exceptionRuleId = null, string? ruleType = null)
        => new(false, 0, 0, attachment, "Invalid", message, policyRuleId, exceptionRuleId, ruleType);
}
