using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Contracts;
using Workforce.Contracts;
using WorkAllocation.Domain;
using WorkAllocation.Infrastructure;

namespace WorkAllocation.Application.Allocations.CancelAllocation;

public sealed record CancelAllocationCommand(Guid AllocationId);

public sealed class CancelAllocationHandler(WorkAllocationDbContext db, IWorkforceQueries workforce, IProjectLookup projects)
    : ICommandHandler<CancelAllocationCommand, Result<AllocationResponse>>
{
    public async Task<Result<AllocationResponse>> HandleAsync(CancelAllocationCommand command)
    {
        var allocation = await db.Allocations.FirstOrDefaultAsync(a => a.Id == command.AllocationId);
        if (allocation is null) return Result<AllocationResponse>.Failure(AllocationErrors.NotFound);

        // Cancelled -> Cancelled n'est pas une transition (11-state-diagrams.md, §2).
        if (allocation.Status == AllocationStatus.Cancelled) return Result<AllocationResponse>.Failure(AllocationErrors.Cancelled);

        allocation.Cancel();
        await db.SaveChangesAsync();

        var responses = await AllocationMapper.ToResponsesAsync([allocation], workforce, projects);
        return Result<AllocationResponse>.Success(responses[0]);
    }
}