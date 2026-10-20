using WorkAllocation.Domain;
using Xunit;

namespace Chrona.WorkAllocation.Tests.Domain;

public class AllocationTests
{
    private static AllocationPeriod P(string start, string end) => new(DateOnly.Parse(start), DateOnly.Parse(end));

    private static Allocation NewAllocation() =>
        new(Guid.NewGuid(), Guid.NewGuid(), P("2026-08-03", "2026-08-28"), new AllocationPercentage(50m));

    [Fact]
    public void Create_StartsActive_AndWritesTheFirstHistoryEntry()
    {
        var allocation = NewAllocation();

        Assert.NotEqual(Guid.Empty, allocation.Id);
        Assert.Equal(AllocationStatus.Active, allocation.Status);
        var entry = Assert.Single(allocation.History);
        Assert.Equal(AllocationChangeType.Created, entry.ChangeType);
        Assert.Null(entry.OldValue);
    }

    [Fact]
    public void Period_WithEndBeforeStart_Throws() =>
        Assert.Throws<ArgumentException>(() => P("2026-08-28", "2026-08-03"));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.01)]
    [InlineData(33.333)]
    public void Percentage_OutOfRangeOrTooPrecise_Throws(double value) =>
        Assert.Throws<ArgumentException>(() => new AllocationPercentage((decimal)value));

    [Fact]
    public void ChangePercentage_RecordsOldAndNewValues_OnlyWhenItActuallyChanges()
    {
        var allocation = NewAllocation();

        allocation.ChangePercentage(new AllocationPercentage(50m)); // identique : aucune entrée
        allocation.ChangePercentage(new AllocationPercentage(75m));

        Assert.Equal(2, allocation.History.Count);
        var change = allocation.History.Last();
        Assert.Equal(AllocationChangeType.PercentageChanged, change.ChangeType);
        Assert.Equal("50", change.OldValue);
        Assert.Equal("75", change.NewValue);
    }

    [Fact]
    public void ChangePeriod_RecordsTheChange()
    {
        var allocation = NewAllocation();

        allocation.ChangePeriod(P("2026-09-01", "2026-09-30"));

        Assert.Equal(new DateOnly(2026, 9, 1), allocation.PeriodStart);
        Assert.Equal(AllocationChangeType.PeriodChanged, allocation.History.Last().ChangeType);
    }

    [Fact]
    public void Cancel_IsTerminal_AndBlocksFurtherChanges()
    {
        var allocation = NewAllocation();

        allocation.Cancel();

        Assert.Equal(AllocationStatus.Cancelled, allocation.Status);
        Assert.Equal(AllocationChangeType.StatusChanged, allocation.History.Last().ChangeType);
        Assert.Throws<InvalidOperationException>(() => allocation.Cancel());
        Assert.Throws<InvalidOperationException>(() => allocation.ChangePercentage(new AllocationPercentage(10m)));
        Assert.Throws<InvalidOperationException>(() => allocation.ChangePeriod(P("2026-10-01", "2026-10-31")));
    }
}