using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using TimeManagement.Domain;
using TimeManagement.Infrastructure;
using WorkAllocation.Contracts;
using Workforce.Contracts;

namespace TimeManagement.Application.Timesheets.SubmitTimesheet;

public sealed record SubmitTimesheetCommand(Guid SubjectId, Guid TimesheetId);

public sealed class SubmitTimesheetHandler(TimeManagementDbContext db, IWorkforceQueries workforce, IAllocationLookup allocations)
    : ICommandHandler<SubmitTimesheetCommand, Result<TimesheetResponse>>
{
    public async Task<Result<TimesheetResponse>> HandleAsync(SubmitTimesheetCommand command)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await TimesheetLock.AcquireAsync(db, command.TimesheetId);

        var loaded = await TimesheetAccess.LoadOwnedAsync(db, workforce, command.SubjectId, command.TimesheetId);
        if (loaded.IsFailure) return Result<TimesheetResponse>.Failure(loaded.Error!);
        var timesheet = loaded.Value;

        if (timesheet.Status != TimesheetStatus.Draft) return Result<TimesheetResponse>.Failure(TimesheetErrors.NotDraft);
        if (timesheet.Entries.Count == 0) return Result<TimesheetResponse>.Failure(TimesheetErrors.Empty);

        timesheet.Submit();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Result<TimesheetResponse>.Success(await TimesheetMapper.ToResponseAsync(timesheet, allocations));
    }
}