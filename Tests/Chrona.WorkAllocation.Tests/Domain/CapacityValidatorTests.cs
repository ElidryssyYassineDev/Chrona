using WorkAllocation.Domain;
using Xunit;

namespace Chrona.WorkAllocation.Tests.Domain;

public class CapacityValidatorTests
{
    private static AllocationPeriod P(string start, string end) => new(DateOnly.Parse(start), DateOnly.Parse(end));

    private static (AllocationPeriod, decimal)[] Existing(params (string Start, string End, decimal Pct)[] items) =>
        items.Select(i => (P(i.Start, i.End), i.Pct)).ToArray();

    [Fact]
    public void NoOtherAllocation_PeakIsTheProposedPercentage() =>
        Assert.Equal(40m, CapacityValidator.PeakLoad(P("2026-08-03", "2026-08-28"), 40m, []));

    [Fact]
    public void ExactlyFullCapacity_IsAllowed()
    {
        var others = Existing(("2026-08-03", "2026-08-28", 60m));

        Assert.False(CapacityValidator.Exceeds(P("2026-08-03", "2026-08-28"), 40m, others));
    }

    [Fact]
    public void OneUnitOverCapacity_IsRejected()
    {
        var others = Existing(("2026-08-03", "2026-08-28", 60m));

        Assert.True(CapacityValidator.Exceeds(P("2026-08-03", "2026-08-28"), 40.01m, others));
    }

    [Fact]
    public void PartialOverlap_IsRejectedWhenTheOverlapExceeds100()
    {
        var others = Existing(("2026-08-01", "2026-08-15", 60m));

        Assert.True(CapacityValidator.Exceeds(P("2026-08-10", "2026-08-25"), 50m, others));
    }

    [Fact]
    public void AdjacentNonOverlappingPeriods_BothFitAt100()
    {
        var others = Existing(("2026-08-01", "2026-08-15", 100m));

        Assert.False(CapacityValidator.Exceeds(P("2026-08-16", "2026-08-31"), 100m, others));
    }

    [Fact]
    public void SharedBoundaryDay_CountsAsOverlap()
    {
        var others = Existing(("2026-08-01", "2026-08-15", 100m));

        Assert.True(CapacityValidator.Exceeds(P("2026-08-15", "2026-08-20"), 1m, others));
    }

    [Fact]
    public void SuccessiveAllocations_DoNotAddUp()
    {
        // A 60 % puis B 60 % ne se superposent jamais : la charge maximale d'une nouvelle allocation à 40 % est 100 %.
        var others = Existing(("2026-08-01", "2026-08-15", 60m), ("2026-08-16", "2026-08-31", 60m));
        var month = P("2026-08-01", "2026-08-31");

        Assert.False(CapacityValidator.Exceeds(month, 40m, others));
        Assert.True(CapacityValidator.Exceeds(month, 41m, others));
    }

    [Fact]
    public void ShortAllocationInsideTheProposedPeriod_IsDetected()
    {
        var others = Existing(("2026-08-10", "2026-08-12", 30m));

        Assert.True(CapacityValidator.Exceeds(P("2026-08-01", "2026-08-31"), 80m, others));
    }

    [Fact]
    public void ZeroProposed_ReturnsTheExistingPeak()
    {
        var others = Existing(("2026-08-01", "2026-08-15", 60m), ("2026-08-10", "2026-08-20", 30m));

        Assert.Equal(90m, CapacityValidator.PeakLoad(P("2026-08-01", "2026-08-31"), 0m, others));
    }
}