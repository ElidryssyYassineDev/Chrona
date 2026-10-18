using ProjectManagement.Domain;
using Xunit;

namespace Chrona.ProjectManagement.Tests.Domain;

public class ProjectTests
{
    [Fact]
    public void Create_StartsActive_WithTrimmedNameAndGeneratedId()
    {
        var project = new Project("  Apollo  ");

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("Apollo", project.Name);
        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.Null(project.ArchivedAtUtc);
        Assert.Empty(project.Members);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_Throws(string? name) =>
        Assert.Throws<ArgumentException>(() => new Project(name!));

    [Fact]
    public void Rename_WhenActive_ChangesName()
    {
        var project = new Project("Apollo");
        project.Rename(" Gemini ");
        Assert.Equal("Gemini", project.Name);
    }

    [Fact]
    public void Archive_SetsStatusAndTimestamp_ThenFreezesTheProject()
    {
        var project = new Project("Apollo");

        project.Archive();

        Assert.Equal(ProjectStatus.Archived, project.Status);
        Assert.NotNull(project.ArchivedAtUtc);
        Assert.Throws<InvalidOperationException>(() => project.Archive());
        Assert.Throws<InvalidOperationException>(() => project.Rename("Other"));
        Assert.Throws<InvalidOperationException>(() => project.AddMember(Guid.NewGuid()));
    }

    [Fact]
    public void AddMember_RecordsMembership_AndRejectsDuplicates()
    {
        var project = new Project("Apollo");
        var employeeId = Guid.NewGuid();

        var member = project.AddMember(employeeId);

        Assert.Equal(project.Id, member.ProjectId);
        Assert.Equal(employeeId, member.EmployeeId);
        Assert.Single(project.Members);
        Assert.Throws<InvalidOperationException>(() => project.AddMember(employeeId));
    }

    [Fact]
    public void AddMember_WithEmptyId_Throws() =>
        Assert.Throws<ArgumentException>(() => new Project("Apollo").AddMember(Guid.Empty));

    [Fact]
    public void RemoveMember_RemovesIt_AndRejectsUnknownEmployee()
    {
        var project = new Project("Apollo");
        var employeeId = Guid.NewGuid();
        project.AddMember(employeeId);

        project.RemoveMember(employeeId);

        Assert.Empty(project.Members);
        Assert.Throws<InvalidOperationException>(() => project.RemoveMember(employeeId));
    }

    [Fact]
    public void RemoveMember_WhenArchived_Throws()
    {
        var project = new Project("Apollo");
        var employeeId = Guid.NewGuid();
        project.AddMember(employeeId);
        project.Archive();

        Assert.Throws<InvalidOperationException>(() => project.RemoveMember(employeeId));
        Assert.Single(project.Members); // les adhésions existantes ne sont pas touchées
    }
}