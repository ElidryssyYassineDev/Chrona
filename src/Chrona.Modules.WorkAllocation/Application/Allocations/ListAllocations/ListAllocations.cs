using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Contracts;
using Workforce.Contracts;
using WorkAllocation.Infrastructure;

namespace WorkAllocation.Application.Allocations.ListAllocations;

public sealed record ListAllocationsQuery(Caller Caller, Guid? EmployeeId, Guid? ProjectId);

public sealed class ListAllocationsHandler(WorkAllocationDbContext db, IWorkforceQueries workforce, IProjectLookup projects)
    : IQueryHandler<ListAllocationsQuery, IReadOnlyList<AllocationResponse>>
{
    public async Task<IReadOnlyList<AllocationResponse>> HandleAsync(ListAllocationsQuery query)
    {
        var allocations = db.Allocations.AsNoTracking();

        if (query.Caller.IsManager)
        {
            if (query.EmployeeId is { } employeeId)
                allocations = allocations.Where(a => a.EmployeeId == employeeId);
        }
        else
        {
            // Un Employee ne voit que les siennes, quels que soient les filtres demandés.
            var own = await workforce.GetEmployeeIdForPrincipalAsync(query.Caller.SubjectId);
            if (own is null) return [];
            var ownId = own.Value;
            allocations = allocations.Where(a => a.EmployeeId == ownId);
        }

        if (query.ProjectId is { } projectId)
            allocations = allocations.Where(a => a.ProjectId == projectId);

        var rows = await allocations.OrderByDescending(a => a.PeriodStart).ThenBy(a => a.Id).ToListAsync();
        return await AllocationMapper.ToResponsesAsync(rows, workforce, projects);
    }
}