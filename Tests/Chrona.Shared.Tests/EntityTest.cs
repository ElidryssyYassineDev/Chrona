using Chrona.Shared.Domain;
using Xunit;

namespace Chrona.Shared.Tests;

public class EntityTest
{
    [Fact]
    public void Entities_WithSameId_ShouldBeEqual()
    {
        // Arrange
        Guid guid = Guid.NewGuid();

        TestEntity entityTest1 = new TestEntity(guid);
        TestEntity entityTest2 = new TestEntity(guid);

        // Assert
        Assert.True(entityTest1.Equals(entityTest2));
        Assert.True(entityTest1 == entityTest2);
    }
}

// Concrete class used only for testing the abstract Entity class
public class TestEntity : Entity
{
    public TestEntity(Guid id) : base(id)
    {
    }
}