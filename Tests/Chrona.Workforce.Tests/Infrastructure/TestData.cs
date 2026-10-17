using Workforce.Application.Departments;
using Workforce.Application.Departments.CreateDepartment;
using Workforce.Application.Employees;
using Workforce.Application.Employees.CreateEmployee;
using Xunit;

namespace Chrona.Workforce.Tests.Infrastructure;

internal static class TestData
{
    public static async Task<DepartmentResponse> NewDepartmentAsync(PostgresFixture db, string? name = null)
    {
        var result = await db.RunAsync(c =>
            new CreateDepartmentHandler(c).HandleAsync(new CreateDepartmentCommand(name ?? $"Dept-{Guid.NewGuid():N}")));
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }

    public static async Task<EmployeeResponse> NewEmployeeAsync(
        PostgresFixture db, Guid departmentId, Guid? managerId = null, Guid? subjectId = null)
    {
        var result = await db.RunAsync(c => new CreateEmployeeHandler(c).HandleAsync(
            new CreateEmployeeCommand(subjectId ?? Guid.NewGuid(), "Alice", "Nguyen", departmentId, managerId)));
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }
}