using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using TimeManagement.Domain;
using TimeManagement.Infrastructure;
using WorkAllocation.Contracts;
using Workforce.Contracts;

namespace TimeManagement.Application.Timesheets;

public sealed record TimeEntryResponse(
    Guid TimeEntryId, Guid AllocationId, string ProjectName, DateOnly EntryDate, decimal Hours, DateTimeOffset CreatedAtUtc);

public sealed record TimesheetResponse(
    Guid TimesheetId,
    Guid EmployeeId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Status,
    DateTimeOffset? LastSubmittedAtUtc,
    decimal TotalHours,
    IReadOnlyList<TimeEntryResponse> Entries);

public sealed record TimesheetSummaryResponse(
    Guid TimesheetId, DateOnly PeriodStart, DateOnly PeriodEnd, string Status,
    DateTimeOffset? LastSubmittedAtUtc, int EntryCount, decimal TotalHours);

internal static class TimesheetErrors
{
    public static readonly Error EmployeeNotFound =
        new("employee_not_found", "No employee record exists for this account.", ErrorKind.NotFound);

    public static readonly Error NotFound =
        new("not_found", "No timesheet was found with the given id.", ErrorKind.NotFound);

    public static readonly Error EntryNotFound =
        new("time_entry_not_found", "No time entry was found with the given id.", ErrorKind.NotFound);

    public static readonly Error NotDraft =
        new("timesheet_not_draft", "Only a timesheet in Draft status can be changed or submitted.", ErrorKind.Conflict);

    public static readonly Error Empty =
        new("timesheet_empty", "An empty timesheet cannot be submitted.", ErrorKind.Conflict);

    public static readonly Error OutsideTimesheetPeriod =
        new("entry_outside_timesheet_period", "The entry date must fall within the timesheet's period.", ErrorKind.Conflict);

    public static readonly Error OutsideAllocationPeriod =
        new("entry_outside_allocation_period", "The entry date must fall within the allocation's period.", ErrorKind.Conflict);

    public static readonly Error AllocationNotActive =
        new("allocation_not_active", "Time can only be recorded against an active allocation.", ErrorKind.Conflict);

    public static readonly Error AllocationInvalid =
        new("validation_error", "allocationId must reference one of your allocations.", ErrorKind.Validation, "allocationId");

    public static readonly Error HoursInvalid =
        new("validation_error", "hours must be greater than 0 and at most 24, with at most 2 decimal places.", ErrorKind.Validation, "hours");

    public static readonly Error PeriodRequired =
        new("validation_error", "periodStart must be provided.", ErrorKind.Validation, "periodStart");

    public static readonly Error PeriodInvalid =
        new("validation_error", "periodEnd must be on or after periodStart.", ErrorKind.Validation, "periodEnd");

    public static readonly Error StatusInvalid =
        new("validation_error", "status must be Draft, Submitted or Approved.", ErrorKind.Validation, "status");
}

internal static class TimesheetAccess
{
    // Propriétaire seul en M5 : une feuille d'un autre donne 404 ; la visibilité Manager arrive en M6 (IsManagerOf).
    public static async Task<Result<Timesheet>> LoadOwnedAsync(
        TimeManagementDbContext db, IWorkforceQueries workforce, Guid subjectId, Guid timesheetId)
    {
        var employeeId = await workforce.GetEmployeeIdForPrincipalAsync(subjectId);
        if (employeeId is null) return Result<Timesheet>.Failure(TimesheetErrors.EmployeeNotFound);

        var owner = employeeId.Value;
        var timesheet = await db.Timesheets.Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == timesheetId && t.EmployeeId == owner);

        return timesheet is null
            ? Result<Timesheet>.Failure(TimesheetErrors.NotFound)
            : Result<Timesheet>.Success(timesheet);
    }
}

internal static class EntryRules
{
    // Les deux contrôles de 06-domain-model.md, §3 sont indépendants : date dans la période de la feuille ET
    // allocation active couvrant la date. Elle doit aussi appartenir à l'employé de la feuille (03-module-design.md).
    public static async Task<Result<AllocationSummary>> ValidateAsync(
        Timesheet timesheet, Guid allocationId, DateOnly entryDate, decimal hours, IAllocationLookup allocations)
    {
        if (!TimeEntry.IsValidHours(hours))
            return Result<AllocationSummary>.Failure(TimesheetErrors.HoursInvalid);
        if (!timesheet.Covers(entryDate))
            return Result<AllocationSummary>.Failure(TimesheetErrors.OutsideTimesheetPeriod);

        var summaries = await allocations.GetAllocationSummariesAsync([allocationId]);
        if (!summaries.TryGetValue(allocationId, out var allocation) || allocation.EmployeeId != timesheet.EmployeeId)
            return Result<AllocationSummary>.Failure(TimesheetErrors.AllocationInvalid);
        if (!allocation.IsActive)
            return Result<AllocationSummary>.Failure(TimesheetErrors.AllocationNotActive);
        if (!allocation.CoversDate(entryDate))
            return Result<AllocationSummary>.Failure(TimesheetErrors.OutsideAllocationPeriod);

        return Result<AllocationSummary>.Success(allocation);
    }
}

internal static class TimesheetMapper
{
    public static async Task<TimesheetResponse> ToResponseAsync(Timesheet timesheet, IAllocationLookup allocations)
    {
        var summaries = await allocations.GetAllocationSummariesAsync(
            timesheet.Entries.Select(e => e.AllocationId).Distinct().ToArray());

        var entries = timesheet.Entries
            .OrderBy(e => e.EntryDate).ThenBy(e => e.CreatedAtUtc)
            .Select(e => ToEntryResponse(e, summaries))
            .ToList();

        return new TimesheetResponse(
            timesheet.Id,
            timesheet.EmployeeId,
            timesheet.PeriodStart,
            timesheet.PeriodEnd,
            timesheet.Status.ToString(),
            timesheet.LastSubmittedAtUtc,
            entries.Sum(e => e.Hours),
            entries);
    }

    public static TimeEntryResponse ToEntryResponse(TimeEntry entry, IReadOnlyDictionary<Guid, AllocationSummary> summaries) =>
        new(
            entry.Id,
            entry.AllocationId,
            summaries.TryGetValue(entry.AllocationId, out var allocation) ? allocation.ProjectName : "Unknown project",
            entry.EntryDate,
            entry.Hours,
            entry.CreatedAtUtc);
}