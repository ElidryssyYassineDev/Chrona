using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Projects;
using ProjectManagement.Domain;
using ProjectManagement.Infrastructure;

namespace ProjectManagement.Application.Members.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(Guid ProjectId, Guid EmployeeId);

public sealed class RemoveProjectMemberHandler(ProjectManagementDbContext db)
    : ICommandHandler<RemoveProjectMemberCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveProjectMemberCommand command)
    {
        var project = await db.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == command.ProjectId);
        if (project is null) return Result.Failure(ProjectErrors.NotFound);
        if (project.Status == ProjectStatus.Archived) return Result.Failure(ProjectErrors.Archived);

        if (project.Members.All(m => m.EmployeeId != command.EmployeeId))
            return Result.Failure(ProjectErrors.NotAMember);

        var member = project.RemoveMember(command.EmployeeId);
        db.Remove(member); // suppression explicite : ne dépend pas de la détection d'orphelins
        await db.SaveChangesAsync();

        return Result.Success();
    }
}