using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Contracts;
using Workforce.Contracts;
using WorkAllocation.Domain;
using WorkAllocation.Infrastructure;

namespace WorkAllocation.Application.Allocations.CreateAllocation;

public sealed record CreateAllocationCommand(
    Guid EmployeeId, Guid ProjectId, DateOnly PeriodStart, DateOnly PeriodEnd, decimal Percentage);

public sealed class CreateAllocationHandler(WorkAllocationDbContext db, IWorkforceQueries workforce, IProjectLookup projects)
    : ICommandHandler<CreateAllocationCommand, Result<AllocationResponse>>
{
    public async Task<Result<AllocationResponse>> HandleAsync(CreateAllocationCommand command)
    {
        if (AllocationInput.Validate(command.PeriodStart, command.PeriodEnd, command.Percentage) is { } invalid)
            return Fail(invalid);

        // Existence et activité vérifiées avant la capacité (09-sequence-diagrams.md, §3).
        if (!await workforce.EmployeeExistsAndIsActiveAsync(command.EmployeeId)) return Fail(AllocationErrors.EmployeeInvalid);
        if (!await projects.ProjectExistsAndIsActiveAsync(command.ProjectId)) return Fail(AllocationErrors.ProjectInvalid);

        var period = new AllocationPeriod(command.PeriodStart, command.PeriodEnd);
        var percentage = new AllocationPercentage(command.Percentage);

        await using var transaction = await db.Database.BeginTransactionAsync();
        await EmployeeCapacityLock.AcquireAsync(db, command.EmployeeId);

        var peak = await CapacityCheck.PeakLoadAsync(db, command.EmployeeId, period, percentage.Value, excludeAllocationId: null);
        if (peak > CapacityValidator.MaxCapacity) return Fail(AllocationErrors.CapacityExceeded);

        var allocation = new Allocation(command.EmployeeId, command.ProjectId, period, percentage);
        db.Allocations.Add(allocation); // l'allocation et sa première entrée d'historique sont insérées ensemble
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var responses = await AllocationMapper.ToResponsesAsync([allocation], workforce, projects);
        return Result<AllocationResponse>.Success(responses[0]);
    }

    private static Result<AllocationResponse> Fail(Error error) => Result<AllocationResponse>.Failure(error);
}