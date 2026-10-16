using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Infrastructure;

namespace Workforce.Application.Departments.ListDepartments;

public sealed record ListDepartmentsQuery;

public sealed class ListDepartmentsHandler(WorkforceDbContext db)
    : IQueryHandler<ListDepartmentsQuery, IReadOnlyList<DepartmentResponse>>
{
    public async Task<IReadOnlyList<DepartmentResponse>> HandleAsync(ListDepartmentsQuery query)
    {
        return await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name, d.CreatedAtUtc))
            .ToListAsync();
    }
}