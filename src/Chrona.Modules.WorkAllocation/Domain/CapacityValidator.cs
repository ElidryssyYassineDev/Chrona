namespace WorkAllocation.Domain;

// Service de domaine sans état (06-domain-model.md, §6). Il évalue la charge de POINTE sur la période,
// jour par jour : deux allocations successives ne s'additionnent pas.
public static class CapacityValidator
{
    public const decimal MaxCapacity = 100m;

    public static decimal PeakLoad(
        AllocationPeriod period,
        decimal proposedPercentage,
        IEnumerable<(AllocationPeriod Period, decimal Percentage)> others)
    {
        // Balayage : +pourcentage au premier jour couvert, -pourcentage le lendemain du dernier jour couvert.
        var events = new List<(int Day, decimal Delta)>();
        foreach (var (otherPeriod, otherPercentage) in others)
        {
            if (!period.Overlaps(otherPeriod)) continue;

            var start = Math.Max(period.Start.DayNumber, otherPeriod.Start.DayNumber);
            var end = Math.Min(period.End.DayNumber, otherPeriod.End.DayNumber);
            events.Add((start, otherPercentage));
            events.Add((end + 1, -otherPercentage));
        }

        var load = proposedPercentage;
        var peak = load;
        foreach (var day in events.GroupBy(e => e.Day).OrderBy(g => g.Key))
        {
            load += day.Sum(e => e.Delta);
            peak = Math.Max(peak, load);
        }

        return peak;
    }

    public static bool Exceeds(
        AllocationPeriod period,
        decimal proposedPercentage,
        IEnumerable<(AllocationPeriod Period, decimal Percentage)> others) =>
        PeakLoad(period, proposedPercentage, others) > MaxCapacity;
}