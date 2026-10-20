using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using TimeManagement.Domain;
using TimeManagement.Infrastructure;
using Workforce.Contracts;

namespace TimeManagement.Application.Timesheets.ListTimesheets;

public sealed record ListTimesheetsQuery(Guid SubjectId, string? Status);

public sealed class ListTimesheetsHandler(TimeManagementDbContext db, IWorkforceQueries workforce)
    : IQueryHandler<ListTimesheetsQuery, Result<IReadOnlyList<TimesheetSummaryResponse>>>
{
    public async Task<Result<IReadOnlyList<TimesheetSummaryResponse>>> HandleAsync(ListTimesheetsQuery query)
    {
        TimesheetStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<TimesheetStatus>(query.Status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                return Result<IReadOnlyList<TimesheetSummaryResponse>>.Failure(TimesheetErrors.StatusInvalid);
            status = parsed;
        }

        var employeeId = await workforce.GetEmployeeIdForPrincipalAsync(query.SubjectId);
        if (employeeId is null)
            return Result<IReadOnlyList<TimesheetSummaryResponse>>.Failure(TimesheetErrors.EmployeeNotFound);
        var owner = employeeId.Value;

        var timesheets = db.Timesheets.AsNoTracking().Where(t => t.EmployeeId == owner);
        if (status is { } wanted)
        {
            timesheets = timesheets.Where(t => t.Status == wanted);
        }

        var rows = await timesheets
            .OrderByDescending(t => t.PeriodStart)
            .Select(t => new
            {
                t.Id,
                t.PeriodStart,
                t.PeriodEnd,
                t.Status,
                t.LastSubmittedAtUtc,
                EntryCount = t.Entries.Count(),
                TotalHours = t.Entries.Sum(e => (decimal?)e.Hours) ?? 0m
            })
            .ToListAsync();

        IReadOnlyList<TimesheetSummaryResponse> summaries = rows
            .Select(r => new TimesheetSummaryResponse(
                r.Id, r.PeriodStart, r.PeriodEnd, r.Status.ToString(), r.LastSubmittedAtUtc, r.EntryCount, r.TotalHours))
            .ToList();

        return Result<IReadOnlyList<TimesheetSummaryResponse>>.Success(summaries);
    }
}