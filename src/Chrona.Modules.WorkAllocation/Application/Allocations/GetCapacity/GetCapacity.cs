using Chrona.Shared.Application;
using WorkAllocation.Domain;
using WorkAllocation.Infrastructure;

namespace WorkAllocation.Application.Allocations.GetCapacity;

public sealed record GetCapacityQuery(Guid EmployeeId, DateOnly PeriodStart, DateOnly PeriodEnd, Guid? ExcludeAllocationId);

// Retour de capacité en direct du formulaire : réutilise le même validateur que la création.
public sealed class GetCapacityHandler(WorkAllocationDbContext db)
    : IQueryHandler<GetCapacityQuery, Result<CapacityResponse>>
{
    public async Task<Result<CapacityResponse>> HandleAsync(GetCapacityQuery query)
    {
        if (query.PeriodStart == default || query.PeriodEnd < query.PeriodStart)
            return Result<CapacityResponse>.Failure(new Error(
                "validation_error", "periodEnd must be on or after periodStart.", ErrorKind.Validation, "periodEnd"));

        var period = new AllocationPeriod(query.PeriodStart, query.PeriodEnd);
        var peak = await CapacityCheck.PeakLoadAsync(db, query.EmployeeId, period, 0m, query.ExcludeAllocationId);

        return Result<CapacityResponse>.Success(new CapacityResponse(
            query.EmployeeId,
            query.PeriodStart,
            query.PeriodEnd,
            peak,
            Math.Max(0m, CapacityValidator.MaxCapacity - peak)));
    }
}