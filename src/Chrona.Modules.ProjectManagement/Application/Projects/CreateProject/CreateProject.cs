using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Domain;
using ProjectManagement.Infrastructure;

namespace ProjectManagement.Application.Projects.CreateProject;

public sealed record CreateProjectCommand(string Name);

public sealed class CreateProjectHandler(ProjectManagementDbContext db)
    : ICommandHandler<CreateProjectCommand, Result<ProjectResponse>>
{
    public async Task<Result<ProjectResponse>> HandleAsync(CreateProjectCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return Fail(ProjectErrors.NameRequired);
        var name = command.Name.Trim();

        if (await db.Projects.AnyAsync(p => p.Name == name)) return Fail(ProjectErrors.NameTaken(name));

        var project = new Project(name);
        db.Projects.Add(project);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Fail(ProjectErrors.NameTaken(name)); // course : la contrainte UNIQUE tranche
        }

        return Result<ProjectResponse>.Success((await ProjectProjection.GetAsync(db, project.Id))!);
    }

    private static Result<ProjectResponse> Fail(Error error) => Result<ProjectResponse>.Failure(error);
}