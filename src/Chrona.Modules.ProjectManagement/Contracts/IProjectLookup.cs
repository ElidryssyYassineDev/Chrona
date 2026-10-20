namespace ProjectManagement.Contracts;

public sealed record ProjectSummary(Guid ProjectId, string Name, bool IsActive);

// Capacités publiques de Project Management pour les autres modules (03-module-design.md, §3).
public interface IProjectLookup
{
    Task<bool> ProjectExistsAndIsActiveAsync(Guid projectId);
    Task<IReadOnlyDictionary<Guid, ProjectSummary>> GetProjectSummariesAsync(IReadOnlyCollection<Guid> projectIds);
}