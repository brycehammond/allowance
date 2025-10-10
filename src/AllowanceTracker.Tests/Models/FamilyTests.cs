using AllowanceTracker.Models;
using FluentAssertions;
using Xunit;

namespace AllowanceTracker.Tests.Models;

public class FamilyTests
{
    [Fact]
    public void Family_ShouldAllowSettingId()
    {
        // Arrange
        var familyId = Guid.NewGuid();
        var family = new Family { Id = familyId };

        // Act & Assert
        family.Id.Should().Be(familyId);
    }

    [Fact]
    public void Family_ShouldHaveName()
    {
        // Arrange
        var family = new Family { Name = "Smith Family" };

        // Act & Assert
        family.Name.Should().Be("Smith Family");
    }

    [Fact]
    public void Family_ShouldInitializeWithEmptyName()
    {
        // Arrange & Act
        var family = new Family();

        // Assert
        family.Name.Should().BeEmpty();
    }

    [Fact]
    public void Family_ShouldHaveCreatedAt()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var family = new Family();
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        family.CreatedAt.Should().BeAfter(beforeCreation);
        family.CreatedAt.Should().BeBefore(afterCreation);
    }

    [Fact]
    public void Family_ShouldAllowSettingCreatedAt()
    {
        // Arrange
        var specificDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var family = new Family { CreatedAt = specificDate };

        // Act & Assert
        family.CreatedAt.Should().Be(specificDate);
    }

    [Fact]
    public void Family_ShouldInitializeWithEmptyMembersCollection()
    {
        // Arrange & Act
        var family = new Family();

        // Assert
        family.Members.Should().NotBeNull();
        family.Members.Should().BeEmpty();
    }

    [Fact]
    public void Family_ShouldAllowAddingMembers()
    {
        // Arrange
        var family = new Family();
        var user = new ApplicationUser { FirstName = "John", LastName = "Doe" };

        // Act
        family.Members.Add(user);

        // Assert
        family.Members.Should().HaveCount(1);
        family.Members.Should().Contain(user);
    }

    [Fact]
    public void Family_ShouldInitializeWithEmptyChildrenCollection()
    {
        // Arrange & Act
        var family = new Family();

        // Assert
        family.Children.Should().NotBeNull();
        family.Children.Should().BeEmpty();
    }

    [Fact]
    public void Family_ShouldAllowAddingChildren()
    {
        // Arrange
        var family = new Family();
        var child = new Child { UserId = Guid.NewGuid(), FamilyId = family.Id };

        // Act
        family.Children.Add(child);

        // Assert
        family.Children.Should().HaveCount(1);
        family.Children.Should().Contain(child);
    }

    [Fact]
    public void Family_ShouldSupportMultipleMembers()
    {
        // Arrange
        var family = new Family();
        var parent1 = new ApplicationUser { FirstName = "John", LastName = "Doe", Role = UserRole.Parent };
        var parent2 = new ApplicationUser { FirstName = "Jane", LastName = "Doe", Role = UserRole.Parent };
        var childUser = new ApplicationUser { FirstName = "Jimmy", LastName = "Doe", Role = UserRole.Child };

        // Act
        family.Members.Add(parent1);
        family.Members.Add(parent2);
        family.Members.Add(childUser);

        // Assert
        family.Members.Should().HaveCount(3);
    }

    [Fact]
    public void Family_ShouldSupportMultipleChildren()
    {
        // Arrange
        var family = new Family();
        var child1 = new Child { UserId = Guid.NewGuid(), FamilyId = family.Id };
        var child2 = new Child { UserId = Guid.NewGuid(), FamilyId = family.Id };

        // Act
        family.Children.Add(child1);
        family.Children.Add(child2);

        // Assert
        family.Children.Should().HaveCount(2);
    }
}
