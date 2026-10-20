using Microsoft.EntityFrameworkCore;
using Workforce.Contracts;

namespace Workforce.Infrastructure;

public sealed class WorkforceQueries(WorkforceDbContext db) : IWorkforceQueries
{
    public Task<bool> EmployeeExistsAndIsActiveAsync(Guid employeeId) =>
        db.Employees.AnyAsync(e => e.Id == employeeId && e.IsActive);

    public Task<Guid?> GetEmployeeIdForPrincipalAsync(Guid subjectId) =>
        db.Employees.AsNoTracking()
            .Where(e => e.KeycloakSubjectId == subjectId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyDictionary<Guid, EmployeeSummary>> GetEmployeeSummariesAsync(
        IReadOnlyCollection<Guid> employeeIds)
    {
        var ids = employeeIds.ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, EmployeeSummary>();

        return await db.Employees.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new EmployeeSummary(e.Id, e.FirstName, e.LastName, e.IsActive))
            .ToDictionaryAsync(s => s.EmployeeId);
    }
}