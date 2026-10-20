using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Contracts;
using Workforce.Contracts;
using WorkAllocation.Infrastructure;

namespace WorkAllocation.Application.Allocations.GetAllocation;

public sealed record GetAllocationQuery(Guid AllocationId, Caller Caller);

public sealed class GetAllocationHandler(WorkAllocationDbContext db, IWorkforceQueries workforce, IProjectLookup projects)
    : IQueryHandler<GetAllocationQuery, Result<AllocationDetailResponse>>
{
    public async Task<Result<AllocationDetailResponse>> HandleAsync(GetAllocationQuery query)
    {
        var allocation = await db.Allocations.AsNoTracking().Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == query.AllocationId);
        if (allocation is null) return Result<AllocationDetailResponse>.Failure(AllocationErrors.NotFound);

        if (!query.Caller.IsManager)
        {
            // Hors portée = 404, pour ne pas révéler l'existence de l'allocation d'un autre.
            var ownEmployeeId = await workforce.GetEmployeeIdForPrincipalAsync(query.Caller.SubjectId);
            if (ownEmployeeId != allocation.EmployeeId)
                return Result<AllocationDetailResponse>.Failure(AllocationErrors.NotFound);
        }

        var response = (await AllocationMapper.ToResponsesAsync([allocation], workforce, projects))[0];
        var history = allocation.History
            .OrderBy(h => h.ChangedAtUtc)
            .Select(h => new AllocationHistoryResponse(h.ChangeType.ToString(), h.OldValue, h.NewValue, h.ChangedAtUtc))
            .ToList();

        return Result<AllocationDetailResponse>.Success(new AllocationDetailResponse(response, history));
    }
}