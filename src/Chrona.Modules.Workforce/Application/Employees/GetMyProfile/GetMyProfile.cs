using Chrona.Shared.Application;
using Microsoft.EntityFrameworkCore;
using Workforce.Infrastructure;

namespace Workforce.Application.Employees.GetMyProfile;

public sealed record GetMyProfileQuery(Guid SubjectId);

public sealed class GetMyProfileHandler(WorkforceDbContext db)
    : IQueryHandler<GetMyProfileQuery, EmployeeResponse?>
{
    public async Task<EmployeeResponse?> HandleAsync(GetMyProfileQuery query) =>
        await EmployeeProjection.Query(db, e => e.KeycloakSubjectId == query.SubjectId).FirstOrDefaultAsync();
}