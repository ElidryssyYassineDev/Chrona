using Chrona.Shared.Application;
using Workforce.Domain;

namespace Workforce.Application.Departments;

public sealed record DepartmentResponse(Guid DepartmentId, string Name, DateTimeOffset CreatedAtUtc)
{
    internal static DepartmentResponse From(Department d) => new(d.Id, d.Name, d.CreatedAtUtc);
}

internal static class DepartmentErrors
{
    public static readonly Error NameRequired =
        new("validation_error", "Department name must not be empty.", ErrorKind.Validation, "name");

    public static readonly Error NotFound =
        new("not_found", "No department was found with the given id.", ErrorKind.NotFound);

    public static readonly Error NotEmpty =
        new("department_not_empty", "This department still has employees assigned to it.", ErrorKind.Conflict);

    public static Error NameTaken(string name) =>
        new("department_name_taken", $"A department named '{name}' already exists.", ErrorKind.Conflict);
}