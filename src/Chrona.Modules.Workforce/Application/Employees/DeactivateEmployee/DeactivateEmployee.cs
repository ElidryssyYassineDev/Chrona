using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Contracts;
using Workforce.Infrastructure;

namespace Workforce.Application.Employees.DeactivateEmployee;

public sealed record DeactivateEmployeeCommand(Guid EmployeeId);

public sealed class DeactivateEmployeeHandler(WorkforceDbContext db, IDomainEventPublisher events)
    : ICommandHandler<DeactivateEmployeeCommand, Result>
{
    public async Task<Result> HandleAsync(DeactivateEmployeeCommand command)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == command.EmployeeId);
        if (employee is null) return Result.Failure(EmployeeErrors.NotFound);

        employee.Deactivate(); // idempotent
        await db.SaveChangesAsync();

        // Publié à chaque appel, même si l'employé était déjà inactif : les consommateurs sont idempotents,
        // donc relancer la désactivation répare un échec partiel (pas d'outbox en v1).
        await events.PublishAsync(new EmployeeDeactivated(employee.Id, employee.DeactivatedAtUtc!.Value));
        return Result.Success();
    }
}