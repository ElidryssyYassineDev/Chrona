using Chrona.Shared.Domain;

namespace TimeManagement.Domain;

public class TimeEntry : Entity
{
    public Guid TimesheetId { get; private set; }
    public Guid AllocationId { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public decimal Hours { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private TimeEntry() { } // Réservé à EF Core

    internal TimeEntry(Guid timesheetId, Guid allocationId, DateOnly entryDate, decimal hours)
        : base(Guid.NewGuid())
    {
        TimesheetId = timesheetId;
        AllocationId = allocationId;
        EntryDate = entryDate;
        Hours = hours;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    // 0 < heures <= 24, deux décimales au plus (numeric(4,2) en base).
    public static bool IsValidHours(decimal hours) => hours > 0 && hours <= 24m && decimal.Round(hours, 2) == hours;

    internal void Update(DateOnly entryDate, decimal hours)
    {
        EntryDate = entryDate;
        Hours = hours;
    }
}