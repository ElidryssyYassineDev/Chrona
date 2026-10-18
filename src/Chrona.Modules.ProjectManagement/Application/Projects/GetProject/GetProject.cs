using Chrona.Shared.Application;
using ProjectManagement.Infrastructure;

namespace ProjectManagement.Application.Projects.GetProject;

public sealed record GetProjectQuery(Guid ProjectId);

public sealed class GetProjectHandler(ProjectManagementDbContext db)
    : IQueryHandler<GetProjectQuery, Result<ProjectResponse>>
{
    public async Task<Result<ProjectResponse>> HandleAsync(GetProjectQuery query)
    {
        var project = await ProjectProjection.GetAsync(db, query.ProjectId);
        return project is null
            ? Result<ProjectResponse>.Failure(ProjectErrors.NotFound)
            : Result<ProjectResponse>.Success(project);
    }
}