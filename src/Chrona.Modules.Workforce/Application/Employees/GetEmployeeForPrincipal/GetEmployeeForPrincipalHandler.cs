using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Domain;
using Workforce.Infrastructure;


namespace Workforce.Application.Employees.GetEmployeeForPrincipal;

public sealed class GetEmployeeForPrincipalHandler
    :IQueryHandler<GetEmployeeForPrincipalQuery, Employee?>
{
    private readonly WorkforceDbContext _db;
    
    public GetEmployeeForPrincipalHandler(WorkforceDbContext db)
    {
        _db = db;
    }

    public async Task<Employee?> HandleAsync(GetEmployeeForPrincipalQuery query)
    {
        return await _db.Employees
            .FirstOrDefaultAsync(
                e => e.KeycloakSubjectId == query.SubjectId
            );
    }
}