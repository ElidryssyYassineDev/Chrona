using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Domain;
using ProjectManagement.Infrastructure;

namespace ProjectManagement.Application.Projects.UpdateProject;

public sealed record UpdateProjectCommand(Guid ProjectId, string Name);

public sealed class UpdateProjectHandler(ProjectManagementDbContext db)
    : ICommandHandler<UpdateProjectCommand, Result<ProjectResponse>>
{
    public async Task<Result<ProjectResponse>> HandleAsync(UpdateProjectCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return Fail(ProjectErrors.NameRequired);
        var name = command.Name.Trim();

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == command.ProjectId);
        if (project is null) return Fail(ProjectErrors.NotFound);
        if (project.Status == ProjectStatus.Archived) return Fail(ProjectErrors.Archived);

        if (await db.Projects.AnyAsync(p => p.Id != project.Id && p.Name == name))
            return Fail(ProjectErrors.NameTaken(name));

        project.Rename(name);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Fail(ProjectErrors.NameTaken(name));
        }

        return Result<ProjectResponse>.Success((await ProjectProjection.GetAsync(db, project.Id))!);
    }

    private static Result<ProjectResponse> Fail(Error error) => Result<ProjectResponse>.Failure(error);
}