using Workforce.Domain;
using Xunit;

namespace Chrona.Workforce.Tests.Domain;

public class DepartmentTests
{
    [Fact]
    public void Create_WithValidName_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        const string name = "Engineering";

        // Act
        var department = new Department(name);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, department.Id);
        Assert.Equal(name, department.Name);
        Assert.True(department.CreatedAtUtc >= before && department.CreatedAtUtc <= after);
    }

    [Fact]
    public void Create_WithExplicitId_ShouldSetProvidedId()
    {
        // Arrange
        var customId = Guid.NewGuid();
        const string name = "Design";

        // Act
        var department = new Department(customId, name);

        // Assert
        Assert.Equal(customId, department.Id);
        Assert.Equal(name, department.Name);
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Department(null!));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Department(string.Empty));
        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("   \t  \r\n  ")]
    public void Create_WithWhitespaceOnlyName_ShouldThrowArgumentException(string whitespaceName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Department(whitespaceName));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_ShouldTrimName()
    {
        // Arrange & Act
        var department = new Department("   Quality Assurance   ");

        // Assert
        Assert.Equal("Quality Assurance", department.Name);
    }

    [Fact]
    public void Rename_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var department = new Department("Engineering");

        // Act
        department.Rename("Platform Engineering");

        // Assert
        Assert.Equal("Platform Engineering", department.Name);
    }

    [Fact]
    public void Rename_WithSurroundingWhitespace_ShouldTrimName()
    {
        // Arrange
        var department = new Department("Engineering");

        // Act
        department.Rename("   Platform Engineering   ");

        // Assert
        Assert.Equal("Platform Engineering", department.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Rename_WithInvalidName_ShouldThrowArgumentExceptionAndKeepExistingName(string? invalidName)
    {
        // Arrange
        const string originalName = "Engineering";
        var department = new Department(originalName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => department.Rename(invalidName!));
        Assert.Equal("name", exception.ParamName);
        Assert.Equal(originalName, department.Name);
    }
}
