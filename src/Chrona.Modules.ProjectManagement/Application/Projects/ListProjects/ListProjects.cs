using Chrona.Shared.Application;
using ProjectManagement.Infrastructure;

namespace ProjectManagement.Application.Projects.ListProjects;

public sealed record ListProjectsQuery;

public sealed class ListProjectsHandler(ProjectManagementDbContext db)
    : IQueryHandler<ListProjectsQuery, IReadOnlyList<ProjectResponse>>
{
    public async Task<IReadOnlyList<ProjectResponse>> HandleAsync(ListProjectsQuery query) =>
        await ProjectProjection.ListAsync(db);
}