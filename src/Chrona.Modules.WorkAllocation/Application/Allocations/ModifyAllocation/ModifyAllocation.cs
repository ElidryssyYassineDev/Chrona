using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Contracts;
using Workforce.Contracts;
using WorkAllocation.Domain;
using WorkAllocation.Infrastructure;

namespace WorkAllocation.Application.Allocations.ModifyAllocation;

public sealed record ModifyAllocationCommand(Guid AllocationId, DateOnly PeriodStart, DateOnly PeriodEnd, decimal Percentage);

public sealed class ModifyAllocationHandler(WorkAllocationDbContext db, IWorkforceQueries workforce, IProjectLookup projects)
    : ICommandHandler<ModifyAllocationCommand, Result<AllocationResponse>>
{
    public async Task<Result<AllocationResponse>> HandleAsync(ModifyAllocationCommand command)
    {
        if (AllocationInput.Validate(command.PeriodStart, command.PeriodEnd, command.Percentage) is { } invalid)
            return Fail(invalid);

        var allocation = await db.Allocations.FirstOrDefaultAsync(a => a.Id == command.AllocationId);
        if (allocation is null) return Fail(AllocationErrors.NotFound);
        if (allocation.Status == AllocationStatus.Cancelled) return Fail(AllocationErrors.Cancelled);

        var period = new AllocationPeriod(command.PeriodStart, command.PeriodEnd);
        var percentage = new AllocationPercentage(command.Percentage);

        await using var transaction = await db.Database.BeginTransactionAsync();
        await EmployeeCapacityLock.AcquireAsync(db, allocation.EmployeeId);

        // L'allocation modifiée est exclue du calcul : elle ne se compte pas deux fois.
        var peak = await CapacityCheck.PeakLoadAsync(db, allocation.EmployeeId, period, percentage.Value, allocation.Id);
        if (peak > CapacityValidator.MaxCapacity) return Fail(AllocationErrors.CapacityExceeded);

        allocation.ChangePeriod(period);
        allocation.ChangePercentage(percentage);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var responses = await AllocationMapper.ToResponsesAsync([allocation], workforce, projects);
        return Result<AllocationResponse>.Success(responses[0]);
    }

    private static Result<AllocationResponse> Fail(Error error) => Result<AllocationResponse>.Failure(error);
}