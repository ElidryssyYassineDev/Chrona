using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Infrastructure;

namespace Workforce.Application.Departments.DeleteDepartment;

public sealed record DeleteDepartmentCommand(Guid DepartmentId);

public sealed class DeleteDepartmentHandler(WorkforceDbContext db)
    : ICommandHandler<DeleteDepartmentCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteDepartmentCommand command)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == command.DepartmentId);
        if (department is null)
            return Result.Failure(DepartmentErrors.NotFound);

        if (await db.Employees.AnyAsync(e => e.DepartmentId == department.Id))
            return Result.Failure(DepartmentErrors.NotEmpty);

        db.Departments.Remove(department);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsForeignKeyViolation())
        {
            // Un employé a été rattaché entre le contrôle et la suppression : la FK Restrict protège.
            return Result.Failure(DepartmentErrors.NotEmpty);
        }

        return Result.Success();
    }
}