using Chrona.Shared.Application;

namespace Workforce.Contracts;

// 06-domain-model.md, §7 : seul événement de domaine de la v1.
public sealed record EmployeeDeactivated(Guid EmployeeId, DateTimeOffset DeactivatedAtUtc) : IDomainEvent;