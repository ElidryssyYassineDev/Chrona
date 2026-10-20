using Chrona.Shared.Domain;

namespace TimeManagement.Domain;

public class Timesheet : Entity
{
    private readonly List<TimeEntry> _entries = [];

    public Guid EmployeeId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public TimesheetStatus Status { get; private set; }
    public DateTimeOffset? LastSubmittedAtUtc { get; private set; }
    public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();

    private Timesheet() { } // Réservé à EF Core

    public Timesheet(Guid employeeId, DateOnly periodStart, DateOnly periodEnd) : base(Guid.NewGuid())
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("employeeId cannot be an empty GUID.", nameof(employeeId));
        if (periodStart > periodEnd)
            throw new ArgumentException("Period start must be on or before period end.", nameof(periodEnd));

        EmployeeId = employeeId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Status = TimesheetStatus.Draft;
    }

    // Bornes incluses.
    public bool Covers(DateOnly date) => PeriodStart <= date && date <= PeriodEnd;

    public TimeEntry AddEntry(Guid allocationId, DateOnly entryDate, decimal hours)
    {
        EnsureDraft();
        if (allocationId == Guid.Empty)
            throw new ArgumentException("allocationId cannot be an empty GUID.", nameof(allocationId));
        RequireValidEntry(entryDate, hours);

        var entry = new TimeEntry(Id, allocationId, entryDate, hours);
        _entries.Add(entry);
        return entry;
    }

    public TimeEntry EditEntry(Guid entryId, DateOnly entryDate, decimal hours)
    {
        EnsureDraft();
        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new InvalidOperationException("The time entry does not belong to this timesheet.");
        RequireValidEntry(entryDate, hours);

        entry.Update(entryDate, hours);
        return entry;
    }

    public void Submit()
    {
        EnsureDraft();
        if (_entries.Count == 0)
            throw new InvalidOperationException("An empty timesheet cannot be submitted.");

        Status = TimesheetStatus.Submitted;
        LastSubmittedAtUtc = DateTimeOffset.UtcNow;
    }

    private void RequireValidEntry(DateOnly entryDate, decimal hours)
    {
        if (!Covers(entryDate))
            throw new ArgumentException("Entry date must fall within the timesheet's period.", nameof(entryDate));
        if (!TimeEntry.IsValidHours(hours))
            throw new ArgumentException("Hours must be greater than 0 and at most 24, with at most 2 decimals.", nameof(hours));
    }

    private void EnsureDraft()
    {
        if (Status != TimesheetStatus.Draft)
            throw new InvalidOperationException("Only a timesheet in Draft status can be changed or submitted.");
    }
}