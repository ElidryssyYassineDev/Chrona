namespace WorkAllocation.Contracts;

public sealed record AllocationSummary(
    Guid AllocationId,
    Guid EmployeeId,
    Guid ProjectId,
    string ProjectName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Percentage,
    bool IsActive)
{
    // Bornes incluses, comme dans le Capacity Validator.
    public bool CoversDate(DateOnly date) => PeriodStart <= date && date <= PeriodEnd;
}

// Capacités publiques de Work Allocation pour les autres modules (03-module-design.md, §3).
public interface IAllocationLookup
{
    Task<IReadOnlyDictionary<Guid, AllocationSummary>> GetAllocationSummariesAsync(IReadOnlyCollection<Guid> allocationIds);
}