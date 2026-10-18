using Chrona.ProjectManagement.Tests.Infrastructure;
using Chrona.Shared.Application;
using ProjectManagement.Application.Projects;
using ProjectManagement.Application.Projects.ArchiveProject;
using ProjectManagement.Application.Projects.CreateProject;
using ProjectManagement.Application.Projects.ListProjects;
using ProjectManagement.Application.Projects.UpdateProject;
using Xunit;

namespace Chrona.ProjectManagement.Tests.Application;

[Collection(ProjectDbCollection.Name)]
public class ProjectHandlerTests(PostgresFixture db)
{
    private static void AssertError(Result result, ErrorKind kind, string code)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
    }

    private async Task<ProjectResponse> NewProjectAsync(string? name = null)
    {
        var result = await db.RunAsync((pm, _) =>
            new CreateProjectHandler(pm).HandleAsync(new CreateProjectCommand(name ?? $"Project-{Guid.NewGuid():N}")));
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }

    [Fact]
    public async Task Create_TrimsName_StartsActiveWithNoMembers()
    {
        var name = $"Project-{Guid.NewGuid():N}";
        var project = await NewProjectAsync($"  {name}  ");

        Assert.Equal(name, project.Name);
        Assert.Equal("Active", project.Status);
        Assert.Equal(0, project.MemberCount);
        Assert.Null(project.ArchivedAtUtc);
    }

    [Fact]
    public async Task Create_BlankName_ReturnsValidationError()
    {
        var result = await db.RunAsync((pm, _) => new CreateProjectHandler(pm).HandleAsync(new CreateProjectCommand("  ")));

        AssertError(result, ErrorKind.Validation, "validation_error");
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var existing = await NewProjectAsync();

        var result = await db.RunAsync((pm, _) =>
            new CreateProjectHandler(pm).HandleAsync(new CreateProjectCommand(existing.Name)));

        AssertError(result, ErrorKind.Conflict, "project_name_taken");
    }

    [Fact]
    public async Task Update_Renames()
    {
        var project = await NewProjectAsync();

        var result = await db.RunAsync((pm, _) =>
            new UpdateProjectHandler(pm).HandleAsync(new UpdateProjectCommand(project.ProjectId, "Renamed-" + project.ProjectId.ToString("N"))));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("Renamed-", result.Value.Name);
    }

    [Fact]
    public async Task Update_ToExistingName_ReturnsConflict()
    {
        var a = await NewProjectAsync();
        var b = await NewProjectAsync();

        var result = await db.RunAsync((pm, _) =>
            new UpdateProjectHandler(pm).HandleAsync(new UpdateProjectCommand(b.ProjectId, a.Name)));

        AssertError(result, ErrorKind.Conflict, "project_name_taken");
    }

    [Fact]
    public async Task Update_Unknown_ReturnsNotFound()
    {
        var result = await db.RunAsync((pm, _) =>
            new UpdateProjectHandler(pm).HandleAsync(new UpdateProjectCommand(Guid.NewGuid(), "Whatever")));

        AssertError(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task Archive_SetsStatusAndTimestamp_ThenRejectsEditsAndSecondArchive()
    {
        var project = await NewProjectAsync();

        var archived = await db.RunAsync((pm, _) =>
            new ArchiveProjectHandler(pm).HandleAsync(new ArchiveProjectCommand(project.ProjectId)));
        var rename = await db.RunAsync((pm, _) =>
            new UpdateProjectHandler(pm).HandleAsync(new UpdateProjectCommand(project.ProjectId, "Nope-" + Guid.NewGuid().ToString("N"))));
        var again = await db.RunAsync((pm, _) =>
            new ArchiveProjectHandler(pm).HandleAsync(new ArchiveProjectCommand(project.ProjectId)));

        Assert.True(archived.IsSuccess);
        Assert.Equal("Archived", archived.Value.Status);
        Assert.NotNull(archived.Value.ArchivedAtUtc);
        AssertError(rename, ErrorKind.Conflict, "project_archived");
        AssertError(again, ErrorKind.Conflict, "project_archived");
    }

    [Fact]
    public async Task List_IncludesCreatedProject()
    {
        var project = await NewProjectAsync();

        var list = await db.RunAsync((pm, _) => new ListProjectsHandler(pm).HandleAsync(new ListProjectsQuery()));

        Assert.Contains(list, p => p.ProjectId == project.ProjectId);
    }
}