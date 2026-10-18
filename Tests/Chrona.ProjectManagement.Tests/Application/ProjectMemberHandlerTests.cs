using Chrona.ProjectManagement.Tests.Infrastructure;
using Chrona.Shared.Application;
using Npgsql;
using ProjectManagement.Application.Members.AddProjectMember;
using ProjectManagement.Application.Members.ListProjectMembers;
using ProjectManagement.Application.Members.RemoveProjectMember;
using ProjectManagement.Application.Projects;
using ProjectManagement.Application.Projects.ArchiveProject;
using ProjectManagement.Application.Projects.CreateProject;
using ProjectManagement.Application.Projects.ListProjects;
using Xunit;

namespace Chrona.ProjectManagement.Tests.Application;

[Collection(ProjectDbCollection.Name)]
public class ProjectMemberHandlerTests(PostgresFixture db)
{
    private static void AssertError(Result result, ErrorKind kind, string code, string? field = null)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(field, result.Error.Field);
    }

    private async Task<Guid> NewProjectAsync()
    {
        var result = await db.RunAsync((pm, _) =>
            new CreateProjectHandler(pm).HandleAsync(new CreateProjectCommand($"Project-{Guid.NewGuid():N}")));
        return result.Value.ProjectId;
    }

    private Task<Result<ProjectMemberResponse>> AddAsync(Guid projectId, Guid employeeId) =>
        db.RunAsync((pm, wf) => new AddProjectMemberHandler(pm, wf).HandleAsync(new AddProjectMemberCommand(projectId, employeeId)));

    private Task<Result> RemoveAsync(Guid projectId, Guid employeeId) =>
        db.RunAsync<Result>((pm, _) => new RemoveProjectMemberHandler(pm).HandleAsync(new RemoveProjectMemberCommand(projectId, employeeId)));

    private Task<Result<IReadOnlyList<ProjectMemberResponse>>> ListAsync(Guid projectId) =>
        db.RunAsync((pm, wf) => new ListProjectMembersHandler(pm, wf).HandleAsync(new ListProjectMembersQuery(projectId)));

    [Fact]
    public async Task Add_ValidEmployee_ReturnsNameAndUpdatesMemberCount()
    {
        var projectId = await NewProjectAsync();
        var employeeId = await db.NewEmployeeAsync();

        var result = await AddAsync(projectId, employeeId);
        var projects = await db.RunAsync((pm, _) => new ListProjectsHandler(pm).HandleAsync(new ListProjectsQuery()));

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("Alice Nguyen", result.Value.EmployeeName);
        Assert.Equal(1, projects.Single(p => p.ProjectId == projectId).MemberCount);
    }

    [Fact]
    public async Task Add_UnknownEmployee_ReturnsValidationError()
    {
        var result = await AddAsync(await NewProjectAsync(), Guid.NewGuid());

        AssertError(result, ErrorKind.Validation, "validation_error", "employeeId");
    }

    [Fact]
    public async Task Add_InactiveEmployee_ReturnsValidationError()
    {
        var result = await AddAsync(await NewProjectAsync(), await db.NewEmployeeAsync(active: false));

        AssertError(result, ErrorKind.Validation, "validation_error", "employeeId");
    }

    [Fact]
    public async Task Add_Duplicate_ReturnsConflict()
    {
        var projectId = await NewProjectAsync();
        var employeeId = await db.NewEmployeeAsync();
        await AddAsync(projectId, employeeId);

        var result = await AddAsync(projectId, employeeId);

        AssertError(result, ErrorKind.Conflict, "already_a_member");
    }

    [Fact]
    public async Task Add_UnknownProject_ReturnsNotFound()
    {
        var result = await AddAsync(Guid.NewGuid(), await db.NewEmployeeAsync());

        AssertError(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task Add_ToArchivedProject_ReturnsConflict_AndLeavesExistingMembersAlone()
    {
        var projectId = await NewProjectAsync();
        var existing = await db.NewEmployeeAsync();
        await AddAsync(projectId, existing);
        await db.RunAsync((pm, _) => new ArchiveProjectHandler(pm).HandleAsync(new ArchiveProjectCommand(projectId)));

        var result = await AddAsync(projectId, await db.NewEmployeeAsync());
        var members = await ListAsync(projectId);

        AssertError(result, ErrorKind.Conflict, "project_archived");
        Assert.Single(members.Value);
        Assert.Equal(existing, members.Value[0].EmployeeId);
    }

    [Fact]
    public async Task Remove_DeletesTheMembership_AndAllowsReAdding()
    {
        var projectId = await NewProjectAsync();
        var employeeId = await db.NewEmployeeAsync();
        await AddAsync(projectId, employeeId);

        var removed = await RemoveAsync(projectId, employeeId);
        var afterRemove = await ListAsync(projectId);
        var readded = await AddAsync(projectId, employeeId);

        Assert.True(removed.IsSuccess);
        Assert.Empty(afterRemove.Value);
        Assert.True(readded.IsSuccess); // prouve que la ligne a bien été supprimée en base
    }

    [Fact]
    public async Task Remove_NonMember_ReturnsNotFound()
    {
        var result = await RemoveAsync(await NewProjectAsync(), await db.NewEmployeeAsync());

        AssertError(result, ErrorKind.NotFound, "member_not_found");
    }

    [Fact]
    public async Task Remove_FromArchivedProject_ReturnsConflict()
    {
        var projectId = await NewProjectAsync();
        var employeeId = await db.NewEmployeeAsync();
        await AddAsync(projectId, employeeId);
        await db.RunAsync((pm, _) => new ArchiveProjectHandler(pm).HandleAsync(new ArchiveProjectCommand(projectId)));

        var result = await RemoveAsync(projectId, employeeId);

        AssertError(result, ErrorKind.Conflict, "project_archived");
    }

    [Fact]
    public async Task List_ReturnsMembersOrderedByName()
    {
        var projectId = await NewProjectAsync();
        await AddAsync(projectId, await db.NewEmployeeAsync());
        await AddAsync(projectId, await db.NewEmployeeAsync());

        var result = await ListAsync(projectId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, m => Assert.Equal("Alice Nguyen", m.EmployeeName));
    }

    [Fact]
    public async Task Database_RefusesDeletingAnEmployeeWhoIsAProjectMember()
    {
        // La FK vers employees n'existe pas dans le modèle EF : ce test prouve qu'elle est bien dans la migration.
        var projectId = await NewProjectAsync();
        var employeeId = await db.NewEmployeeAsync();
        await AddAsync(projectId, employeeId);

        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var delete = new NpgsqlCommand("DELETE FROM employees WHERE employee_id = @id", connection);
        delete.Parameters.AddWithValue("id", employeeId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
        Assert.Equal("fk_project_members_employee_id", ex.ConstraintName);
    }
}