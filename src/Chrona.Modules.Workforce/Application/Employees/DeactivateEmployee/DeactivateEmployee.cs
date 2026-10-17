using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Infrastructure;

namespace Workforce.Application.Employees.DeactivateEmployee;

public sealed record DeactivateEmployeeCommand(Guid EmployeeId);

public sealed class DeactivateEmployeeHandler(WorkforceDbContext db)
    : ICommandHandler<DeactivateEmployeeCommand, Result>
{
    public async Task<Result> HandleAsync(DeactivateEmployeeCommand command)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == command.EmployeeId);
        if (employee is null) return Result.Failure(EmployeeErrors.NotFound);

        employee.Deactivate(); // idempotent
        await db.SaveChangesAsync();
        return Result.Success();
        // TODO M4 : publier EmployeeDeactivated quand son consommateur (Work Allocation) existera.
    }
}