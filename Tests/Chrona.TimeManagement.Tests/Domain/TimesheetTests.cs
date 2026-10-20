using TimeManagement.Domain;
using Xunit;

namespace Chrona.TimeManagement.Tests.Domain;

public class TimesheetTests
{
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid AllocationId = Guid.NewGuid();
    private static readonly DateOnly Monday = new(2026, 8, 3);
    private static readonly DateOnly Sunday = new(2026, 8, 9);

    private static Timesheet NewTimesheet() => new(EmployeeId, Monday, Sunday);

    [Fact]
    public void Create_StartsInDraft_WithGeneratedIdAndNoEntries()
    {
        var timesheet = NewTimesheet();

        Assert.NotEqual(Guid.Empty, timesheet.Id);
        Assert.Equal(TimesheetStatus.Draft, timesheet.Status);
        Assert.Null(timesheet.LastSubmittedAtUtc);
        Assert.Empty(timesheet.Entries);
    }

    [Fact]
    public void Create_WithEndBeforeStart_Throws() =>
        Assert.Throws<ArgumentException>(() => new Timesheet(EmployeeId, Sunday, Monday));

    [Fact]
    public void Create_WithEmptyEmployee_Throws() =>
        Assert.Throws<ArgumentException>(() => new Timesheet(Guid.Empty, Monday, Sunday));

    [Fact]
    public void Covers_IncludesBothBoundaries()
    {
        var timesheet = NewTimesheet();

        Assert.True(timesheet.Covers(Monday));
        Assert.True(timesheet.Covers(Sunday));
        Assert.False(timesheet.Covers(Monday.AddDays(-1)));
        Assert.False(timesheet.Covers(Sunday.AddDays(1)));
    }

    [Fact]
    public void AddEntry_InDraft_RecordsIt()
    {
        var timesheet = NewTimesheet();

        var entry = timesheet.AddEntry(AllocationId, Monday.AddDays(2), 7.5m);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(timesheet.Id, entry.TimesheetId);
        Assert.Single(timesheet.Entries);
    }

    [Fact]
    public void AddEntry_OutsideThePeriod_Throws() =>
        Assert.Throws<ArgumentException>(() => NewTimesheet().AddEntry(AllocationId, Sunday.AddDays(1), 8m));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(24.01)]
    [InlineData(1.234)]
    public void AddEntry_WithInvalidHours_Throws(double hours) =>
        Assert.Throws<ArgumentException>(() => NewTimesheet().AddEntry(AllocationId, Monday, (decimal)hours));

    [Fact]
    public void AddEntry_WithExactly24Hours_IsAccepted() =>
        Assert.Single(NewTimesheet().Let(t => t.AddEntry(AllocationId, Monday, 24m)).Timesheet.Entries);

    [Fact]
    public void EditEntry_ChangesDateAndHours()
    {
        var timesheet = NewTimesheet();
        var entry = timesheet.AddEntry(AllocationId, Monday, 4m);

        timesheet.EditEntry(entry.Id, Monday.AddDays(1), 6m);

        Assert.Equal(Monday.AddDays(1), entry.EntryDate);
        Assert.Equal(6m, entry.Hours);
    }

    [Fact]
    public void EditEntry_Unknown_Throws() =>
        Assert.Throws<InvalidOperationException>(() => NewTimesheet().EditEntry(Guid.NewGuid(), Monday, 4m));

    [Fact]
    public void Submit_WithoutEntries_Throws() =>
        Assert.Throws<InvalidOperationException>(() => NewTimesheet().Submit());

    [Fact]
    public void Submit_SetsStatusAndTimestamp_ThenFreezesTheTimesheet()
    {
        var timesheet = NewTimesheet();
        var entry = timesheet.AddEntry(AllocationId, Monday, 8m);

        timesheet.Submit();

        Assert.Equal(TimesheetStatus.Submitted, timesheet.Status);
        Assert.NotNull(timesheet.LastSubmittedAtUtc);
        Assert.Throws<InvalidOperationException>(() => timesheet.Submit());
        Assert.Throws<InvalidOperationException>(() => timesheet.AddEntry(AllocationId, Monday, 1m));
        Assert.Throws<InvalidOperationException>(() => timesheet.EditEntry(entry.Id, Monday, 2m));
    }
}

internal static class TimesheetTestExtensions
{
    // Petit utilitaire de test : applique une action puis renvoie la feuille.
    public static (Timesheet Timesheet, T Result) Let<T>(this Timesheet timesheet, Func<Timesheet, T> action) =>
        (timesheet, action(timesheet));
}