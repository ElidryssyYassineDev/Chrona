using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Domain;
using Workforce.Infrastructure;

namespace Workforce.Application.Employees.CreateEmployee;

public sealed record CreateEmployeeCommand(
    Guid KeycloakSubjectId, string FirstName, string LastName, Guid DepartmentId, Guid? ManagerId);

public sealed class CreateEmployeeHandler(WorkforceDbContext db)
    : ICommandHandler<CreateEmployeeCommand, Result<EmployeeResponse>>
{
    public async Task<Result<EmployeeResponse>> HandleAsync(CreateEmployeeCommand command)
    {
        if (command.KeycloakSubjectId == Guid.Empty) return Fail(EmployeeErrors.Required("keycloakSubjectId"));
        if (string.IsNullOrWhiteSpace(command.FirstName)) return Fail(EmployeeErrors.Required("firstName"));
        if (string.IsNullOrWhiteSpace(command.LastName)) return Fail(EmployeeErrors.Required("lastName"));

        if (!await db.Departments.AnyAsync(d => d.Id == command.DepartmentId))
            return Fail(EmployeeErrors.DepartmentNotFound);

        if (command.ManagerId is { } managerId &&
            !await db.Employees.AnyAsync(e => e.Id == managerId && e.IsActive))
            return Fail(EmployeeErrors.ManagerInvalid);

        if (await db.Employees.AnyAsync(e => e.KeycloakSubjectId == command.KeycloakSubjectId))
            return Fail(EmployeeErrors.AlreadyExists);

        var employee = new Employee(
            command.KeycloakSubjectId, command.FirstName, command.LastName, command.DepartmentId, command.ManagerId);
        db.Employees.Add(employee);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Fail(EmployeeErrors.AlreadyExists); // course entre le contrôle et l'insertion
        }

        var created = await EmployeeProjection.Query(db, e => e.Id == employee.Id).SingleAsync();
        return Result<EmployeeResponse>.Success(created);
    }

    private static Result<EmployeeResponse> Fail(Error error) => Result<EmployeeResponse>.Failure(error);
}