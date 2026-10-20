using Microsoft.EntityFrameworkCore;
using ProjectManagement.Contracts;
using ProjectManagement.Domain;

namespace ProjectManagement.Infrastructure;

public sealed class ProjectLookup(ProjectManagementDbContext db) : IProjectLookup
{
    public Task<bool> ProjectExistsAndIsActiveAsync(Guid projectId) =>
        db.Projects.AnyAsync(p => p.Id == projectId && p.Status == ProjectStatus.Active);

    public async Task<IReadOnlyDictionary<Guid, ProjectSummary>> GetProjectSummariesAsync(
        IReadOnlyCollection<Guid> projectIds)
    {
        var ids = projectIds.ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, ProjectSummary>();

        return await db.Projects.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProjectSummary(p.Id, p.Name, p.Status == ProjectStatus.Active))
            .ToDictionaryAsync(s => s.ProjectId);
    }
}