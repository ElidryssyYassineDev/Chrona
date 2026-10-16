using Workforce.Domain;
using Xunit;

namespace Chrona.Workforce.Tests.Domain;

public class EmployeeTests
{
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldBeActiveWithGeneratedId()
    {
        var before = DateTimeOffset.UtcNow;

        var employee = new Employee(SubjectId, "  Alice ", "Nguyen", DepartmentId);

        Assert.NotEqual(Guid.Empty, employee.Id);
        Assert.Equal("Alice", employee.FirstName);
        Assert.Equal(DepartmentId, employee.DepartmentId);
        Assert.True(employee.IsActive);
        Assert.Null(employee.DeactivatedAtUtc);
        Assert.True(employee.CreatedAtUtc >= before);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidFirstName_ShouldThrow(string? firstName)
    {
        Assert.Throws<ArgumentException>(() => new Employee(SubjectId, firstName!, "Nguyen", DepartmentId));
    }

    [Fact]
    public void Create_WithEmptyDepartment_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new Employee(SubjectId, "Alice", "Nguyen", Guid.Empty));
    }

    [Fact]
    public void Create_WithEmptySubjectId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new Employee(Guid.Empty, "Alice", "Nguyen", DepartmentId));
    }

    [Fact]
    public void UpdateDetails_WithSelfAsManager_ShouldThrowAndLeaveEmployeeUnchanged()
    {
        var employee = new Employee(SubjectId, "Alice", "Nguyen", DepartmentId);

        Assert.Throws<ArgumentException>(() => employee.UpdateDetails("Bob", "Smith", DepartmentId, employee.Id));

        Assert.Equal("Alice", employee.FirstName);
        Assert.Null(employee.ManagerId);
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldApplyAllChanges()
    {
        var employee = new Employee(SubjectId, "Alice", "Nguyen", DepartmentId);
        var newDepartment = Guid.NewGuid();
        var manager = Guid.NewGuid();

        employee.UpdateDetails("Bob", "Smith", newDepartment, manager);

        Assert.Equal("Bob", employee.FirstName);
        Assert.Equal("Smith", employee.LastName);
        Assert.Equal(newDepartment, employee.DepartmentId);
        Assert.Equal(manager, employee.ManagerId);
    }

    [Fact]
    public void Deactivate_ShouldSetInactiveAndTimestamp_AndBeIdempotent()
    {
        var employee = new Employee(SubjectId, "Alice", "Nguyen", DepartmentId);

        employee.Deactivate();
        var firstTimestamp = employee.DeactivatedAtUtc;
        employee.Deactivate();

        Assert.False(employee.IsActive);
        Assert.NotNull(firstTimestamp);
        Assert.Equal(firstTimestamp, employee.DeactivatedAtUtc);
    }
}