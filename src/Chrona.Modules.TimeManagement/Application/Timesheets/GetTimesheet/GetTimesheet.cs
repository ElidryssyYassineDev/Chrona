using Chrona.Shared.Application;
using TimeManagement.Infrastructure;
using WorkAllocation.Contracts;
using Workforce.Contracts;

namespace TimeManagement.Application.Timesheets.GetTimesheet;

public sealed record GetTimesheetQuery(Guid SubjectId, Guid TimesheetId);

public sealed class GetTimesheetHandler(TimeManagementDbContext db, IWorkforceQueries workforce, IAllocationLookup allocations)
    : IQueryHandler<GetTimesheetQuery, Result<TimesheetResponse>>
{
    public async Task<Result<TimesheetResponse>> HandleAsync(GetTimesheetQuery query)
    {
        var loaded = await TimesheetAccess.LoadOwnedAsync(db, workforce, query.SubjectId, query.TimesheetId);
        if (loaded.IsFailure) return Result<TimesheetResponse>.Failure(loaded.Error!);

        return Result<TimesheetResponse>.Success(await TimesheetMapper.ToResponseAsync(loaded.Value, allocations));
    }
}