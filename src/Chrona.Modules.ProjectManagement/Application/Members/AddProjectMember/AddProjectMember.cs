using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Projects;
using ProjectManagement.Domain;
using ProjectManagement.Infrastructure;
using Workforce.Contracts;

namespace ProjectManagement.Application.Members.AddProjectMember;

public sealed record AddProjectMemberCommand(Guid ProjectId, Guid EmployeeId);

public sealed class AddProjectMemberHandler(ProjectManagementDbContext db, IWorkforceQueries workforce)
    : ICommandHandler<AddProjectMemberCommand, Result<ProjectMemberResponse>>
{
    public async Task<Result<ProjectMemberResponse>> HandleAsync(AddProjectMemberCommand command)
    {
        var project = await db.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == command.ProjectId);
        if (project is null) return Fail(ProjectErrors.NotFound);
        if (project.Status == ProjectStatus.Archived) return Fail(ProjectErrors.Archived);

        // Appel réel à Workforce via son contrat : aucune validation dupliquée ici.
        if (!await workforce.EmployeeExistsAndIsActiveAsync(command.EmployeeId))
            return Fail(ProjectErrors.EmployeeInvalid);

        if (project.Members.Any(m => m.EmployeeId == command.EmployeeId))
            return Fail(ProjectErrors.AlreadyMember);

        var member = project.AddMember(command.EmployeeId);
        db.Add(member); // marque explicitement l'adhésion comme ajoutée (clé composite renseignée côté client)

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            return Fail(ProjectErrors.AlreadyMember);
        }
        catch (DbUpdateException ex) when (ex.IsForeignKeyViolation())
        {
            return Fail(ProjectErrors.EmployeeInvalid); // filet de sécurité : la FK vers employees
        }

        var names = await workforce.GetEmployeeSummariesAsync([command.EmployeeId]);
        return Result<ProjectMemberResponse>.Success(ProjectProjection.ToMemberResponse(member, names));
    }

    private static Result<ProjectMemberResponse> Fail(Error error) => Result<ProjectMemberResponse>.Failure(error);
}