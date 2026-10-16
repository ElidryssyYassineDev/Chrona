using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Domain;
using Workforce.Infrastructure;

namespace Workforce.Application.Departments.CreateDepartment;

public sealed record CreateDepartmentCommand(string Name);

public sealed class CreateDepartmentHandler(WorkforceDbContext db)
    : ICommandHandler<CreateDepartmentCommand, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> HandleAsync(CreateDepartmentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<DepartmentResponse>.Failure(DepartmentErrors.NameRequired);

        var name = command.Name.Trim();

        // Contrôle applicatif : rejet rapide et message clair.
        if (await db.Departments.AnyAsync(d => d.Name == name))
            return Result<DepartmentResponse>.Failure(DepartmentErrors.NameTaken(name));

        var department = new Department(name);
        db.Departments.Add(department);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Garantie finale : deux requêtes concurrentes ont passé le contrôle, la contrainte UNIQUE tranche.
            return Result<DepartmentResponse>.Failure(DepartmentErrors.NameTaken(name));
        }

        return Result<DepartmentResponse>.Success(DepartmentResponse.From(department));
    }
}