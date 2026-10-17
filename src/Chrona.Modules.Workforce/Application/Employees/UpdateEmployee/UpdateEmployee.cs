using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Application.Employees;
using Workforce.Infrastructure;

namespace Workforce.Application.Employees.UpdateEmployee;

public sealed record UpdateEmployeeCommand(
    Guid EmployeeId, string FirstName, string LastName, Guid DepartmentId, Guid? ManagerId);

public sealed class UpdateEmployeeHandler(WorkforceDbContext db)
    : ICommandHandler<UpdateEmployeeCommand, Result<EmployeeResponse>>
{
    public async Task<Result<EmployeeResponse>> HandleAsync(UpdateEmployeeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName)) return Fail(EmployeeErrors.Required("firstName"));
        if (string.IsNullOrWhiteSpace(command.LastName)) return Fail(EmployeeErrors.Required("lastName"));

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == command.EmployeeId);
        if (employee is null) return Fail(EmployeeErrors.NotFound);

        if (command.ManagerId == employee.Id) return Fail(EmployeeErrors.SelfManagement);

        if (!await db.Departments.AnyAsync(d => d.Id == command.DepartmentId))
            return Fail(EmployeeErrors.DepartmentNotFound);

        // Un manager inchangé reste accepté même s'il a été désactivé depuis : on ne bloque que les nouvelles affectations.
        if (command.ManagerId is { } managerId && managerId != employee.ManagerId &&
            !await db.Employees.AnyAsync(e => e.Id == managerId && e.IsActive))
            return Fail(EmployeeErrors.ManagerInvalid);

        employee.UpdateDetails(command.FirstName, command.LastName, command.DepartmentId, command.ManagerId);
        await db.SaveChangesAsync();

        var updated = await EmployeeProjection.Query(db, e => e.Id == employee.Id).SingleAsync();
        return Result<EmployeeResponse>.Success(updated);
    }

    private static Result<EmployeeResponse> Fail(Error error) => Result<EmployeeResponse>.Failure(error);
}