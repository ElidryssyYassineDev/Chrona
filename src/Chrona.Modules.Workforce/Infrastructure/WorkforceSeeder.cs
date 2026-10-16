using Microsoft.EntityFrameworkCore;
using Workforce.Domain;

namespace Workforce.Infrastructure;

public static class WorkforceSeeder
{
    public const string DefaultDepartmentName = "General";

    public static async Task SeedAsync(WorkforceDbContext db, Guid adminSubjectId, string firstName, string lastName)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Name == DefaultDepartmentName);
        if (department is null)
        {
            department = new Department(DefaultDepartmentName);
            db.Departments.Add(department);
        }

        if (!await db.Employees.AnyAsync(e => e.KeycloakSubjectId == adminSubjectId))
        {
            db.Employees.Add(new Employee(adminSubjectId, firstName, lastName, department.Id));
        }

        await db.SaveChangesAsync();
    }
}