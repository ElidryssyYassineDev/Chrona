using Chrona.Shared.Domain;

namespace WorkAllocation.Domain;

public class AllocationHistory : Entity
{
    public Guid AllocationId { get; private set; }
    public AllocationChangeType ChangeType { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }

    private AllocationHistory() { } // Réservé à EF Core

    internal AllocationHistory(Guid allocationId, AllocationChangeType changeType, string? oldValue, string? newValue)
        : base(Guid.NewGuid())
    {
        AllocationId = allocationId;
        ChangeType = changeType;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedAtUtc = DateTimeOffset.UtcNow;
    }
}