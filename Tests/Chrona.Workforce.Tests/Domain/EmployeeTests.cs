using Workforce.Domain;

namespace Chrona.Workforce.Tests.Domain;

public class EmployeeTests
{
    private static readonly Guid ValidKeycloakSubjectId = Guid.NewGuid();
    private static readonly Guid ValidDepartmentId = Guid.NewGuid();
    private const string ValidFirstName = "Alice";
    private const string ValidLastName = "Martin";

    private static Employee CreateValidEmployee()
    {
        return new Employee(ValidKeycloakSubjectId, ValidFirstName, ValidLastName, ValidDepartmentId);
    }

    // ───────────────────────────────────────────────
    // Constructor — valid creation
    // ───────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArgs_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var employee = CreateValidEmployee();
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, employee.Id);
        Assert.Equal(ValidKeycloakSubjectId, employee.KeycloakSubjectId);
        Assert.Equal(ValidFirstName, employee.FirstName);
        Assert.Equal(ValidLastName, employee.LastName);
        Assert.Equal(ValidDepartmentId, employee.DepartmentId);
        Assert.True(employee.IsActive);
        Assert.Null(employee.ManagerId);
        Assert.Null(employee.DeactivatedAtUtc);
        Assert.True(employee.CreatedAtUtc >= before && employee.CreatedAtUtc <= after);
        Assert.Empty(employee.DomainEvents);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var employee1 = CreateValidEmployee();
        var employee2 = CreateValidEmployee();

        // Assert
        Assert.NotEqual(employee1.Id, employee2.Id);
    }

    // ───────────────────────────────────────────────
    // Constructor — KeycloakSubjectId validation
    // ───────────────────────────────────────────────

    [Fact]
    public void Create_WithEmptyKeycloakSubjectId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => new Employee(Guid.Empty, ValidFirstName, ValidLastName, ValidDepartmentId));
        Assert.Equal("keycloakSubjectId", exception.ParamName);
    }

    // ───────────────────────────────────────────────
    // Constructor — FirstName validation
    // ───────────────────────────────────────────────

    [Fact]
    public void Create_WithNullFirstName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => new Employee(ValidKeycloakSubjectId, null!, ValidLastName, ValidDepartmentId));
        Assert.Equal("firstName", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyFirstName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => new Employee(ValidKeycloakSubjectId, string.Empty, ValidLastName, ValidDepartmentId));
        Assert.Equal("firstName", exception.ParamName);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("   \t  \r\n  ")]
    public void Create_WithWhitespaceFirstName_ShouldThrowArgumentException(string whitespaceFirstName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => new Employee(ValidKeycloakSubjectId, whitespaceFirstName, ValidLastName, ValidDepartmentId));
        Assert.Equal("firstName", exception.ParamName);
    }

    // ───────────────────────────────────────────────
    // Constructor — LastName validation
    // ───────────────────────────────────────────────

    [Fact]
    public void Create_WithNullLastName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => new Employee(ValidKeycloakSubjectId, ValidFirstName, null!, ValidDepartmentId));
        Assert.Equal("lastName", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyLastName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => new Employee(ValidKeycloakSubjectId, ValidFirstName, string.Empty, ValidDepartmentId));
        Assert.Equal("lastName", exception.ParamName);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("   \t  \r\n  ")]
    public void Create_WithWhitespaceLastName_ShouldThrowArgumentException(string whitespaceLastName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => new Employee(ValidKeycloakSubjectId, ValidFirstName, whitespaceLastName, ValidDepartmentId));
        Assert.Equal("lastName", exception.ParamName);
    }

    // ───────────────────────────────────────────────
    // Constructor — DepartmentId validation
    // ───────────────────────────────────────────────

    [Fact]
    public void Create_WithEmptyDepartmentId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => new Employee(ValidKeycloakSubjectId, ValidFirstName, ValidLastName, Guid.Empty));
        Assert.Equal("departmentId", exception.ParamName);
    }

    // ───────────────────────────────────────────────
    // Constructor — Name trimming
    // ───────────────────────────────────────────────

    [Fact]
    public void Create_WithSurroundingWhitespace_ShouldTrimNames()
    {
        // Act
        var employee = new Employee(
            ValidKeycloakSubjectId, "   Alice   ", "   Martin   ", ValidDepartmentId);

        // Assert
        Assert.Equal("Alice", employee.FirstName);
        Assert.Equal("Martin", employee.LastName);
    }

    // ───────────────────────────────────────────────
    // Deactivate
    // ───────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_ShouldSetInactiveTimestampAndRaiseDomainEvent()
    {
        // Arrange
        var employee = CreateValidEmployee();
        var before = DateTimeOffset.UtcNow;

        // Act
        employee.Deactivate();
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.False(employee.IsActive);
        Assert.NotNull(employee.DeactivatedAtUtc);
        Assert.True(employee.DeactivatedAtUtc >= before && employee.DeactivatedAtUtc <= after);

        var domainEvent = Assert.Single(employee.DomainEvents);
        var deactivatedEvent = Assert.IsType<EmployeeDeactivated>(domainEvent);
        Assert.Equal(employee.Id, deactivatedEvent.EmployeeId);
        Assert.Equal(employee.DeactivatedAtUtc.Value, deactivatedEvent.DeactivatedAtUtc);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldBeIdempotentAndNotRaiseAdditionalEvents()
    {
        // Arrange
        var employee = CreateValidEmployee();
        employee.Deactivate();
        var initialDeactivatedAt = employee.DeactivatedAtUtc;

        // Act
        employee.Deactivate();

        // Assert
        Assert.False(employee.IsActive);
        Assert.Equal(initialDeactivatedAt, employee.DeactivatedAtUtc);
        Assert.Single(employee.DomainEvents);
    }

    // ───────────────────────────────────────────────
    // ChangeDepartment
    // ───────────────────────────────────────────────

    [Fact]
    public void ChangeDepartment_WithValidId_ShouldUpdateDepartmentId()
    {
        // Arrange
        var employee = CreateValidEmployee();
        var newDepartmentId = Guid.NewGuid();

        // Act
        employee.ChangeDepartment(newDepartmentId);

        // Assert
        Assert.Equal(newDepartmentId, employee.DepartmentId);
    }

    [Fact]
    public void ChangeDepartment_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var employee = CreateValidEmployee();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => employee.ChangeDepartment(Guid.Empty));
        Assert.Equal("newDepartmentId", exception.ParamName);
    }

    [Fact]
    public void ChangeDepartment_WithSameDepartmentId_ShouldBeNoOp()
    {
        // Arrange
        var employee = CreateValidEmployee();

        // Act
        employee.ChangeDepartment(ValidDepartmentId);

        // Assert
        Assert.Equal(ValidDepartmentId, employee.DepartmentId);
    }

    // ───────────────────────────────────────────────
    // AssignManager
    // ───────────────────────────────────────────────

    [Fact]
    public void AssignManager_WithValidId_ShouldUpdateManagerId()
    {
        // Arrange
        var employee = CreateValidEmployee();
        var managerId = Guid.NewGuid();

        // Act
        employee.AssignManager(managerId);

        // Assert
        Assert.Equal(managerId, employee.ManagerId);
    }

    [Fact]
    public void AssignManager_WithNull_ShouldClearManagerId()
    {
        // Arrange
        var employee = CreateValidEmployee();
        employee.AssignManager(Guid.NewGuid());

        // Act
        employee.AssignManager(null);

        // Assert
        Assert.Null(employee.ManagerId);
    }

    [Fact]
    public void AssignManager_WithSelfId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var employee = CreateValidEmployee();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => employee.AssignManager(employee.Id));
        Assert.Equal("An employee cannot be their own manager.", exception.Message);
    }

    [Fact]
    public void AssignManager_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var employee = CreateValidEmployee();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => employee.AssignManager(Guid.Empty));
        Assert.Equal("managerId", exception.ParamName);
    }

    [Fact]
    public void AssignManager_WithSameManagerId_ShouldBeNoOp()
    {
        // Arrange
        var employee = CreateValidEmployee();
        var managerId = Guid.NewGuid();
        employee.AssignManager(managerId);

        // Act
        employee.AssignManager(managerId);

        // Assert
        Assert.Equal(managerId, employee.ManagerId);
    }

    // ───────────────────────────────────────────────
    // UpdateName
    // ───────────────────────────────────────────────

    [Fact]
    public void UpdateName_WithValidNames_ShouldUpdateAndTrimProperties()
    {
        // Arrange
        var employee = CreateValidEmployee();

        // Act
        employee.UpdateName("   Bob   ", "   Smith   ");

        // Assert
        Assert.Equal("Bob", employee.FirstName);
        Assert.Equal("Smith", employee.LastName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void UpdateName_WithInvalidFirstName_ShouldThrowArgumentExceptionAndPreserveNames(string? invalidFirstName)
    {
        // Arrange
        var employee = CreateValidEmployee();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => employee.UpdateName(invalidFirstName!, "Smith"));
        Assert.Equal("firstName", exception.ParamName);
        Assert.Equal(ValidFirstName, employee.FirstName);
        Assert.Equal(ValidLastName, employee.LastName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void UpdateName_WithInvalidLastName_ShouldThrowArgumentExceptionAndPreserveNames(string? invalidLastName)
    {
        // Arrange
        var employee = CreateValidEmployee();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => employee.UpdateName("Bob", invalidLastName!));
        Assert.Equal("lastName", exception.ParamName);
        Assert.Equal(ValidFirstName, employee.FirstName);
        Assert.Equal(ValidLastName, employee.LastName);
    }
}
