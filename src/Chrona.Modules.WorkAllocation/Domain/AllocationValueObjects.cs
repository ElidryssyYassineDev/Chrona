namespace WorkAllocation.Domain;

// Bornes incluses : une allocation du 1er au 15 et une autre du 15 au 20 se chevauchent le 15.
public readonly record struct AllocationPeriod
{
    public AllocationPeriod(DateOnly start, DateOnly end)
    {
        if (start > end)
            throw new ArgumentException("Period start must be on or before period end.", nameof(end));

        Start = start;
        End = end;
    }

    public DateOnly Start { get; }
    public DateOnly End { get; }

    public bool Overlaps(AllocationPeriod other) => Start <= other.End && other.Start <= End;

    public override string ToString() => $"{Start:O}..{End:O}";
}

public readonly record struct AllocationPercentage
{
    public AllocationPercentage(decimal value)
    {
        if (value <= 0 || value > 100)
            throw new ArgumentException("Percentage must be greater than 0 and at most 100.", nameof(value));
        // numeric(5,2) en base : au-delà de 2 décimales, la valeur stockée différerait de celle contrôlée.
        if (decimal.Round(value, 2) != value)
            throw new ArgumentException("Percentage can have at most 2 decimal places.", nameof(value));

        Value = value;
    }

    public decimal Value { get; }

    public override string ToString() => Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}