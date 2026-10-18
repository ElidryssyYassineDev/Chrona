using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Domain;
using ProjectManagement.Infrastructure;

namespace ProjectManagement.Application.Projects.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid ProjectId);

public sealed class ArchiveProjectHandler(ProjectManagementDbContext db)
    : ICommandHandler<ArchiveProjectCommand, Result<ProjectResponse>>
{
    public async Task<Result<ProjectResponse>> HandleAsync(ArchiveProjectCommand command)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == command.ProjectId);
        if (project is null) return Result<ProjectResponse>.Failure(ProjectErrors.NotFound);

        // Archived -> Archived n'est pas une transition (11-state-diagrams.md, §4).
        if (project.Status == ProjectStatus.Archived) return Result<ProjectResponse>.Failure(ProjectErrors.Archived);

        project.Archive();
        await db.SaveChangesAsync();

        return Result<ProjectResponse>.Success((await ProjectProjection.GetAsync(db, project.Id))!);
    }
}