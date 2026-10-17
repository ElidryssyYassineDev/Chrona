using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Infrastructure;

namespace Workforce.Application.Employees.GetEmployee;

public sealed record GetEmployeeQuery(Guid EmployeeId);

public sealed class GetEmployeeHandler(WorkforceDbContext db)
    : IQueryHandler<GetEmployeeQuery, Result<EmployeeResponse>>
{
    public async Task<Result<EmployeeResponse>> HandleAsync(GetEmployeeQuery query)
    {
        var employee = await EmployeeProjection.Query(db, e => e.Id == query.EmployeeId).FirstOrDefaultAsync();
        return employee is null
            ? Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound)
            : Result<EmployeeResponse>.Success(employee);
    }
}