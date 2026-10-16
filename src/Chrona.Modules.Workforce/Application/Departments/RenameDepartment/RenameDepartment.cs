using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Infrastructure;

namespace Workforce.Application.Departments.RenameDepartment;

public sealed record RenameDepartmentCommand(Guid DepartmentId, string Name);

public sealed class RenameDepartmentHandler(WorkforceDbContext db)
    : ICommandHandler<RenameDepartmentCommand, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> HandleAsync(RenameDepartmentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<DepartmentResponse>.Failure(DepartmentErrors.NameRequired);

        var name = command.Name.Trim();

        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == command.DepartmentId);
        if (department is null)
            return Result<DepartmentResponse>.Failure(DepartmentErrors.NotFound);

        if (await db.Departments.AnyAsync(d => d.Id != department.Id && d.Name == name))
            return Result<DepartmentResponse>.Failure(DepartmentErrors.NameTaken(name));

        department.Rename(name);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Result<DepartmentResponse>.Failure(DepartmentErrors.NameTaken(name));
        }

        return Result<DepartmentResponse>.Success(DepartmentResponse.From(department));
    }
}