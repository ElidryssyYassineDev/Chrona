using Chrona.Shared.Domain;

namespace WorkAllocation.Domain;

public class Allocation : Entity
{
    private readonly List<AllocationHistory> _history = [];

    public Guid EmployeeId { get; private set; }
    public Guid ProjectId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public decimal Percentage { get; private set; }
    public AllocationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<AllocationHistory> History => _history.AsReadOnly();

    public AllocationPeriod Period => new(PeriodStart, PeriodEnd);

    private Allocation() { } // Réservé à EF Core

    public Allocation(Guid employeeId, Guid projectId, AllocationPeriod period, AllocationPercentage percentage)
        : base(Guid.NewGuid())
    {
        if (employeeId == Guid.Empty) throw new ArgumentException("employeeId cannot be an empty GUID.", nameof(employeeId));
        if (projectId == Guid.Empty) throw new ArgumentException("projectId cannot be an empty GUID.", nameof(projectId));

        EmployeeId = employeeId;
        ProjectId = projectId;
        PeriodStart = period.Start;
        PeriodEnd = period.End;
        Percentage = percentage.Value;
        Status = AllocationStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;

        // La création écrit elle-même la première entrée : une allocation n'est jamais sans historique.
        Record(AllocationChangeType.Created, null, $"{period} @ {percentage}%");
    }

    public void ChangePeriod(AllocationPeriod newPeriod)
    {
        EnsureActive();
        if (newPeriod == Period) return;

        Record(AllocationChangeType.PeriodChanged, Period.ToString(), newPeriod.ToString());
        PeriodStart = newPeriod.Start;
        PeriodEnd = newPeriod.End;
    }

    public void ChangePercentage(AllocationPercentage newPercentage)
    {
        EnsureActive();
        var current = new AllocationPercentage(Percentage);
        if (newPercentage == current) return;

        Record(AllocationChangeType.PercentageChanged, current.ToString(), newPercentage.ToString());
        Percentage = newPercentage.Value;
    }

    public void Cancel()
    {
        EnsureActive();
        Status = AllocationStatus.Cancelled;
        Record(AllocationChangeType.StatusChanged, nameof(AllocationStatus.Active), nameof(AllocationStatus.Cancelled));
    }

    private void Record(AllocationChangeType type, string? oldValue, string? newValue) =>
        _history.Add(new AllocationHistory(Id, type, oldValue, newValue));

    private void EnsureActive()
    {
        if (Status != AllocationStatus.Active)
            throw new InvalidOperationException("A cancelled allocation can no longer be changed.");
    }
}