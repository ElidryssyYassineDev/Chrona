using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Contracts;
using Workforce.Contracts;
using WorkAllocation.Domain;
using WorkAllocation.Infrastructure;

namespace WorkAllocation.Application.Allocations;

public sealed record Caller(Guid SubjectId, bool IsManager);

public sealed record AllocationResponse(
    Guid AllocationId,
    Guid EmployeeId,
    string EmployeeName,
    Guid ProjectId,
    string ProjectName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Percentage,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record AllocationHistoryResponse(string ChangeType, string? OldValue, string? NewValue, DateTimeOffset ChangedAtUtc);

public sealed record AllocationDetailResponse(AllocationResponse Allocation, IReadOnlyList<AllocationHistoryResponse> History);

public sealed record CapacityResponse(
    Guid EmployeeId, DateOnly PeriodStart, DateOnly PeriodEnd, decimal PeakAllocatedPercentage, decimal AvailablePercentage);

internal static class AllocationErrors
{
    public static readonly Error EmployeeInvalid =
        new("validation_error", "employeeId must reference an existing, active employee.", ErrorKind.Validation, "employeeId");

    public static readonly Error ProjectInvalid =
        new("validation_error", "projectId must reference an existing, active project.", ErrorKind.Validation, "projectId");

    public static readonly Error NotFound =
        new("not_found", "No allocation was found with the given id.", ErrorKind.NotFound);

    public static readonly Error CapacityExceeded =
        new("capacity_exceeded", "This allocation would exceed the employee's available capacity.", ErrorKind.Conflict);

    public static readonly Error Cancelled =
        new("allocation_cancelled", "This allocation is cancelled and can no longer be changed.", ErrorKind.Conflict);
}

internal static class AllocationInput
{
    // Validation de forme (400) avant tout accès aux autres modules ou à la base.
    public static Error? Validate(DateOnly start, DateOnly end, decimal percentage)
    {
        if (start == default)
            return new Error("validation_error", "periodStart must be provided.", ErrorKind.Validation, "periodStart");
        if (end < start)
            return new Error("validation_error", "periodEnd must be on or after periodStart.", ErrorKind.Validation, "periodEnd");
        if (percentage <= 0 || percentage > 100)
            return new Error("validation_error", "percentage must be greater than 0 and at most 100.", ErrorKind.Validation, "percentage");
        if (decimal.Round(percentage, 2) != percentage)
            return new Error("validation_error", "percentage must have at most 2 decimal places.", ErrorKind.Validation, "percentage");
        return null;
    }
}

internal static class CapacityCheck
{
    public static async Task<decimal> PeakLoadAsync(
        WorkAllocationDbContext db, Guid employeeId, AllocationPeriod period, decimal proposedPercentage, Guid? excludeAllocationId)
    {
        // Seules les allocations actives de l'employé qui chevauchent la période (index employee_id, status).
        var others = await db.Allocations.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                        && a.Status == AllocationStatus.Active
                        && a.PeriodStart <= period.End
                        && a.PeriodEnd >= period.Start
                        && (excludeAllocationId == null || a.Id != excludeAllocationId))
            .Select(a => new { a.PeriodStart, a.PeriodEnd, a.Percentage })
            .ToListAsync();

        return CapacityValidator.PeakLoad(
            period,
            proposedPercentage,
            others.Select(o => (new AllocationPeriod(o.PeriodStart, o.PeriodEnd), o.Percentage)));
    }
}

internal static class AllocationMapper
{
    // Pas de jointure inter-modules : les noms viennent des contrats Workforce et Project Management.
    public static async Task<List<AllocationResponse>> ToResponsesAsync(
        IReadOnlyCollection<Allocation> allocations, IWorkforceQueries workforce, IProjectLookup projects)
    {
        if (allocations.Count == 0) return [];

        var employees = await workforce.GetEmployeeSummariesAsync(allocations.Select(a => a.EmployeeId).Distinct().ToArray());
        var projectSummaries = await projects.GetProjectSummariesAsync(allocations.Select(a => a.ProjectId).Distinct().ToArray());

        return allocations
            .Select(a => new AllocationResponse(
                a.Id,
                a.EmployeeId,
                employees.TryGetValue(a.EmployeeId, out var employee) ? employee.FullName : "Unknown employee",
                a.ProjectId,
                projectSummaries.TryGetValue(a.ProjectId, out var project) ? project.Name : "Unknown project",
                a.PeriodStart,
                a.PeriodEnd,
                a.Percentage,
                a.Status.ToString(),
                a.CreatedAtUtc))
            .ToList();
    }
}