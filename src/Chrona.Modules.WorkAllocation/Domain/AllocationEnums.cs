namespace WorkAllocation.Domain;

public enum AllocationStatus { Active, Cancelled }

public enum AllocationChangeType { Created, StatusChanged, PeriodChanged, PercentageChanged }