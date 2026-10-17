using System.Linq.Expressions;
using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Domain;
using Workforce.Infrastructure;

namespace Workforce.Application.Employees;

public sealed record EmployeeResponse(
    Guid EmployeeId,
    Guid KeycloakSubjectId,
    string FirstName,
    string LastName,
    Guid DepartmentId,
    string DepartmentName,
    Guid? ManagerId,
    string? ManagerName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DeactivatedAtUtc);

internal static class EmployeeErrors
{
    public static Error Required(string field) =>
        new("validation_error", $"{field} must not be empty.", ErrorKind.Validation, field);

    public static readonly Error NotFound =
        new("not_found", "No employee was found with the given id.", ErrorKind.NotFound);

    public static readonly Error DepartmentNotFound =
        new("validation_error", "departmentId must reference an existing department.", ErrorKind.Validation, "departmentId");

    public static readonly Error ManagerInvalid =
        new("validation_error", "managerId must reference an existing, active employee.", ErrorKind.Validation, "managerId");

    public static readonly Error SelfManagement =
        new("employee_cannot_manage_self", "An employee cannot be their own manager.", ErrorKind.Conflict);

    public static readonly Error AlreadyExists =
        new("employee_already_exists", "An employee is already linked to this Keycloak account.", ErrorKind.Conflict);
}

internal static class EmployeeProjection
{
    // Lecture seule : jointure Department (obligatoire) + jointure gauche sur le manager.
    // Le filtre est appliqué sur l'entité AVANT la projection pour rester traduisible en SQL.
    public static IQueryable<EmployeeResponse> Query(WorkforceDbContext db, Expression<Func<Employee, bool>>? filter = null)
    {
        var employees = db.Employees.AsNoTracking();
        if (filter is not null)
        {
            employees = employees.Where(filter);
        }

        return from e in employees
               join d in db.Departments.AsNoTracking() on e.DepartmentId equals d.Id
               join m in db.Employees.AsNoTracking() on e.ManagerId equals (Guid?)m.Id into managers
               from m in managers.DefaultIfEmpty()
               orderby e.LastName, e.FirstName
               select new EmployeeResponse(
                   e.Id,
                   e.KeycloakSubjectId,
                   e.FirstName,
                   e.LastName,
                   e.DepartmentId,
                   d.Name,
                   e.ManagerId,
                   m == null ? null : m.FirstName + " " + m.LastName,
                   e.IsActive,
                   e.CreatedAtUtc,
                   e.DeactivatedAtUtc);
    }
}