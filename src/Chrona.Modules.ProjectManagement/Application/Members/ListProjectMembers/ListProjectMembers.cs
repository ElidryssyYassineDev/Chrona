using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Projects;
using ProjectManagement.Infrastructure;
using Workforce.Contracts;

namespace ProjectManagement.Application.Members.ListProjectMembers;

public sealed record ListProjectMembersQuery(Guid ProjectId);

public sealed class ListProjectMembersHandler(ProjectManagementDbContext db, IWorkforceQueries workforce)
    : IQueryHandler<ListProjectMembersQuery, Result<IReadOnlyList<ProjectMemberResponse>>>
{
    public async Task<Result<IReadOnlyList<ProjectMemberResponse>>> HandleAsync(ListProjectMembersQuery query)
    {
        var project = await db.Projects.AsNoTracking().Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == query.ProjectId);
        if (project is null) return Result<IReadOnlyList<ProjectMemberResponse>>.Failure(ProjectErrors.NotFound);

        // Pas de jointure inter-modules : les noms viennent du contrat Workforce.
        var names = await workforce.GetEmployeeSummariesAsync(project.Members.Select(m => m.EmployeeId).ToArray());

        IReadOnlyList<ProjectMemberResponse> members = project.Members
            .Select(m => ProjectProjection.ToMemberResponse(m, names))
            .OrderBy(m => m.EmployeeName)
            .ToList();

        return Result<IReadOnlyList<ProjectMemberResponse>>.Success(members);
    }
}