using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Infrastructure;

namespace Workforce.Application.Employees.ListEmployees;

public sealed record ListEmployeesQuery;

public sealed class ListEmployeesHandler(WorkforceDbContext db)
    : IQueryHandler<ListEmployeesQuery, IReadOnlyList<EmployeeResponse>>
{
    public async Task<IReadOnlyList<EmployeeResponse>> HandleAsync(ListEmployeesQuery query) =>
        await EmployeeProjection.Query(db).ToListAsync();
}