using Chrona.Shared.Application;
using Chrona.Workforce.Tests.Infrastructure;
using Workforce.Application.Employees.CreateEmployee;
using Workforce.Application.Employees.DeactivateEmployee;
using Workforce.Application.Employees.GetEmployee;
using Workforce.Application.Employees.GetMyProfile;
using Workforce.Application.Employees.ListEmployees;
using Workforce.Application.Employees.UpdateEmployee;
using Xunit;

namespace Chrona.Workforce.Tests.Application;

[Collection(WorkforceDbCollection.Name)]
public class EmployeeHandlerTests(PostgresFixture db)
{
    private static void AssertError(Result result, ErrorKind kind, string code, string? field = null)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(field, result.Error.Field);
    }

    private Task<Result> DeactivateAsync(Guid id) =>
        db.RunAsync<Result>(c => new DeactivateEmployeeHandler(c).HandleAsync(new DeactivateEmployeeCommand(id)));

    [Fact]
    public async Task Create_ReturnsDepartmentAndManagerNames()
    {
        var department = await TestData.NewDepartmentAsync(db);
        var manager = await TestData.NewEmployeeAsync(db, department.DepartmentId);

        var employee = await TestData.NewEmployeeAsync(db, department.DepartmentId, manager.EmployeeId);

        Assert.Equal(department.Name, employee.DepartmentName);
        Assert.Equal("Alice Nguyen", employee.ManagerName);
        Assert.True(employee.IsActive);
    }

    [Fact]
    public async Task Create_UnknownDepartment_ReturnsValidationError()
    {
        var result = await db.RunAsync(c => new CreateEmployeeHandler(c).HandleAsync(
            new CreateEmployeeCommand(Guid.NewGuid(), "Alice", "Nguyen", Guid.NewGuid(), null)));

        AssertError(result, ErrorKind.Validation, "validation_error", "departmentId");
    }

    [Fact]
    public async Task Create_UnknownManager_ReturnsValidationError()
    {
        var department = await TestData.NewDepartmentAsync(db);

        var result = await db.RunAsync(c => new CreateEmployeeHandler(c).HandleAsync(
            new CreateEmployeeCommand(Guid.NewGuid(), "Alice", "Nguyen", department.DepartmentId, Guid.NewGuid())));

        AssertError(result, ErrorKind.Validation, "validation_error", "managerId");
    }

    [Fact]
    public async Task Create_InactiveManager_ReturnsValidationError()
    {
        var department = await TestData.NewDepartmentAsync(db);
        var manager = await TestData.NewEmployeeAsync(db, department.DepartmentId);
        await DeactivateAsync(manager.EmployeeId);

        var result = await db.RunAsync(c => new CreateEmployeeHandler(c).HandleAsync(
            new CreateEmployeeCommand(Guid.NewGuid(), "Bob", "Martin", department.DepartmentId, manager.EmployeeId)));

        AssertError(result, ErrorKind.Validation, "validation_error", "managerId");
    }

    [Fact]
    public async Task Create_DuplicateKeycloakAccount_ReturnsConflict()
    {
        var department = await TestData.NewDepartmentAsync(db);
        var subject = Guid.NewGuid();
        await TestData.NewEmployeeAsync(db, department.DepartmentId, subjectId: subject);

        var result = await db.RunAsync(c => new CreateEmployeeHandler(c).HandleAsync(
            new CreateEmployeeCommand(subject, "Bob", "Martin", department.DepartmentId, null)));

        AssertError(result, ErrorKind.Conflict, "employee_already_exists");
    }

    [Fact]
    public async Task Update_SelfAsManager_ReturnsConflict()
    {
        var department = await TestData.NewDepartmentAsync(db);
        var employee = await TestData.NewEmployeeAsync(db, department.DepartmentId);

        var result = await db.RunAsync(c => new UpdateEmployeeHandler(c).HandleAsync(new UpdateEmployeeCommand(
            employee.EmployeeId, "Alice", "Nguyen", department.DepartmentId, employee.EmployeeId)));

        AssertError(result, ErrorKind.Conflict, "employee_cannot_manage_self");
    }

    [Fact]
    public async Task Update_ChangesDepartmentManagerAndNames()
    {
        var oldDepartment = await TestData.NewDepartmentAsync(db);
        var newDepartment = await TestData.NewDepartmentAsync(db);
        var manager = await TestData.NewEmployeeAsync(db, oldDepartment.DepartmentId);
        var employee = await TestData.NewEmployeeAsync(db, oldDepartment.DepartmentId);

        var result = await db.RunAsync(c => new UpdateEmployeeHandler(c).HandleAsync(new UpdateEmployeeCommand(
            employee.EmployeeId, "Bob", "Martin", newDepartment.DepartmentId, manager.EmployeeId)));

        Assert.True(result.IsSuccess);
        Assert.Equal("Bob", result.Value.FirstName);
        Assert.Equal(newDepartment.Name, result.Value.DepartmentName);
        Assert.Equal(manager.EmployeeId, result.Value.ManagerId);
    }

    [Fact]
    public async Task Update_KeepingAManagerDeactivatedSince_Succeeds()
    {
        var department = await TestData.NewDepartmentAsync(db);
        var manager = await TestData.NewEmployeeAsync(db, department.DepartmentId);
        var employee = await TestData.NewEmployeeAsync(db, department.DepartmentId, manager.EmployeeId);
        await DeactivateAsync(manager.EmployeeId);

        var result = await db.RunAsync(c => new UpdateEmployeeHandler(c).HandleAsync(new UpdateEmployeeCommand(
            employee.EmployeeId, "Alice", "Renamed", department.DepartmentId, manager.EmployeeId)));

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", result.Value.LastName);
    }

    [Fact]
    public async Task Deactivate_IsIdempotent_AndKeepsFirstTimestamp()
    {
        var department = await TestData.NewDepartmentAsync(db);
        var employee = await TestData.NewEmployeeAsync(db, department.DepartmentId);

        var first = await DeactivateAsync(employee.EmployeeId);
        var afterFirst = await db.RunAsync(c => new GetEmployeeHandler(c).HandleAsync(new GetEmployeeQuery(employee.EmployeeId)));
        var second = await DeactivateAsync(employee.EmployeeId);
        var afterSecond = await db.RunAsync(c => new GetEmployeeHandler(c).HandleAsync(new GetEmployeeQuery(employee.EmployeeId)));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.False(afterSecond.Value.IsActive);
        Assert.NotNull(afterFirst.Value.DeactivatedAtUtc);
        Assert.Equal(afterFirst.Value.DeactivatedAtUtc, afterSecond.Value.DeactivatedAtUtc);
    }

    [Fact]
    public async Task Deactivate_Unknown_ReturnsNotFound()
    {
        var result = await DeactivateAsync(Guid.NewGuid());

        AssertError(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task List_IncludesCreatedEmployee()
    {
        var department = await TestData.NewDepartmentAsync(db);
        var employee = await TestData.NewEmployeeAsync(db, department.DepartmentId);

        var list = await db.RunAsync(c => new ListEmployeesHandler(c).HandleAsync(new ListEmployeesQuery()));

        Assert.Contains(list, e => e.EmployeeId == employee.EmployeeId);
    }

    [Fact]
    public async Task GetMyProfile_ResolvesBySubject_AndReturnsNullWhenUnknown()
    {
        var department = await TestData.NewDepartmentAsync(db);
        var subject = Guid.NewGuid();
        var employee = await TestData.NewEmployeeAsync(db, department.DepartmentId, subjectId: subject);

        var found = await db.RunAsync(c => new GetMyProfileHandler(c).HandleAsync(new GetMyProfileQuery(subject)));
        var missing = await db.RunAsync(c => new GetMyProfileHandler(c).HandleAsync(new GetMyProfileQuery(Guid.NewGuid())));

        Assert.Equal(employee.EmployeeId, found?.EmployeeId);
        Assert.Null(missing);
    }
}