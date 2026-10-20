using Microsoft.EntityFrameworkCore;
using ProjectManagement.Contracts;
using WorkAllocation.Contracts;
using WorkAllocation.Domain;

namespace WorkAllocation.Infrastructure;

public sealed class AllocationLookup(WorkAllocationDbContext db, IProjectLookup projects) : IAllocationLookup
{
    public async Task<IReadOnlyDictionary<Guid, AllocationSummary>> GetAllocationSummariesAsync(
        IReadOnlyCollection<Guid> allocationIds)
    {
        var ids = allocationIds.ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, AllocationSummary>();

        var rows = await db.Allocations.AsNoTracking().Where(a => ids.Contains(a.Id)).ToListAsync();

        // Pas de jointure inter-modules : le nom du projet vient du contrat Project Management.
        var projectSummaries = await projects.GetProjectSummariesAsync(rows.Select(r => r.ProjectId).Distinct().ToArray());

        return rows.ToDictionary(
            a => a.Id,
            a => new AllocationSummary(
                a.Id,
                a.EmployeeId,
                a.ProjectId,
                projectSummaries.TryGetValue(a.ProjectId, out var project) ? project.Name : "Unknown project",
                a.PeriodStart,
                a.PeriodEnd,
                a.Percentage,
                a.Status == AllocationStatus.Active));
    }
}