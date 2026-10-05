using Chrona.Shared.Domain;

namespace Workforce.Domain;

public sealed record EmployeeDeactivated(Guid EmployeeId, DateTimeOffset DeactivatedAtUtc) : IDomainEvent;