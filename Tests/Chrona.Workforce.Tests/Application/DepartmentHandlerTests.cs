using Chrona.Shared.Application;
using Chrona.Workforce.Tests.Infrastructure;
using Workforce.Application.Departments.CreateDepartment;
using Workforce.Application.Departments.DeleteDepartment;
using Workforce.Application.Departments.ListDepartments;
using Workforce.Application.Departments.RenameDepartment;
using Xunit;

namespace Chrona.Workforce.Tests.Application;

[Collection(WorkforceDbCollection.Name)]
public class DepartmentHandlerTests(PostgresFixture db)
{
    private static void AssertError(Result result, ErrorKind kind, string code)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
    }

    [Fact]
    public async Task Create_TrimsName()
    {
        var name = $"Dept-{Guid.NewGuid():N}";
        var result = await db.RunAsync(c =>
            new CreateDepartmentHandler(c).HandleAsync(new CreateDepartmentCommand($"  {name}  ")));

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
    }

    [Fact]
    public async Task Create_BlankName_ReturnsValidationError()
    {
        var result = await db.RunAsync(c =>
            new CreateDepartmentHandler(c).HandleAsync(new CreateDepartmentCommand("   ")));

        AssertError(result, ErrorKind.Validation, "validation_error");
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var existing = await TestData.NewDepartmentAsync(db);

        var result = await db.RunAsync(c =>
            new CreateDepartmentHandler(c).HandleAsync(new CreateDepartmentCommand(existing.Name)));

        AssertError(result, ErrorKind.Conflict, "department_name_taken");
    }

    [Fact]
    public async Task Rename_ToExistingName_ReturnsConflict()
    {
        var a = await TestData.NewDepartmentAsync(db);
        var b = await TestData.NewDepartmentAsync(db);

        var result = await db.RunAsync(c =>
            new RenameDepartmentHandler(c).HandleAsync(new RenameDepartmentCommand(b.DepartmentId, a.Name)));

        AssertError(result, ErrorKind.Conflict, "department_name_taken");
    }

    [Fact]
    public async Task Rename_Unknown_ReturnsNotFound()
    {
        var result = await db.RunAsync(c =>
            new RenameDepartmentHandler(c).HandleAsync(new RenameDepartmentCommand(Guid.NewGuid(), "Whatever")));

        AssertError(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task Delete_DepartmentWithEmployees_ReturnsConflict()
    {
        var department = await TestData.NewDepartmentAsync(db);
        await TestData.NewEmployeeAsync(db, department.DepartmentId);

        var result = await db.RunAsync(c =>
            new DeleteDepartmentHandler(c).HandleAsync(new DeleteDepartmentCommand(department.DepartmentId)));

        AssertError(result, ErrorKind.Conflict, "department_not_empty");
    }

    [Fact]
    public async Task Delete_EmptyDepartment_RemovesIt()
    {
        var department = await TestData.NewDepartmentAsync(db);

        var result = await db.RunAsync(c =>
            new DeleteDepartmentHandler(c).HandleAsync(new DeleteDepartmentCommand(department.DepartmentId)));
        var list = await db.RunAsync(c => new ListDepartmentsHandler(c).HandleAsync(new ListDepartmentsQuery()));

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(list, d => d.DepartmentId == department.DepartmentId);
    }
}