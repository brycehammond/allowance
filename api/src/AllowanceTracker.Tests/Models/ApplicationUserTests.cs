using AllowanceTracker.Models;
using FluentAssertions;
using Xunit;

namespace AllowanceTracker.Tests.Models;

public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_ShouldAllowSettingGuidId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId };

        // Act & Assert
        user.Id.Should().Be(userId);
    }

    [Fact]
    public void ApplicationUser_ShouldHaveFirstName()
    {
        // Arrange
        var user = new ApplicationUser { FirstName = "John" };

        // Act & Assert
        user.FirstName.Should().Be("John");
    }

    [Fact]
    public void ApplicationUser_ShouldHaveLastName()
    {
        // Arrange
        var user = new ApplicationUser { LastName = "Doe" };

        // Act & Assert
        user.LastName.Should().Be("Doe");
    }

    [Fact]
    public void ApplicationUser_ShouldReturnFullName()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void ApplicationUser_ShouldHaveDefaultRoleAsParent()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.Role.Should().Be(UserRole.Parent);
    }

    [Fact]
    public void ApplicationUser_ShouldAllowSettingRoleToChild()
    {
        // Arrange
        var user = new ApplicationUser { Role = UserRole.Child };

        // Act & Assert
        user.Role.Should().Be(UserRole.Child);
    }

    [Fact]
    public void ApplicationUser_ShouldHaveNullableFamilyId()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act & Assert
        user.FamilyId.Should().BeNull();
    }

    [Fact]
    public void ApplicationUser_ShouldAllowSettingFamilyId()
    {
        // Arrange
        var familyId = Guid.NewGuid();
        var user = new ApplicationUser { FamilyId = familyId };

        // Act & Assert
        user.FamilyId.Should().Be(familyId);
    }

    [Fact]
    public void ApplicationUser_ShouldHaveNullFamilyByDefault()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.Family.Should().BeNull();
    }

    [Fact]
    public void ApplicationUser_ShouldHaveNullChildProfileByDefault()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.ChildProfile.Should().BeNull();
    }

    [Fact]
    public void ApplicationUser_ShouldInitializeWithEmptyFirstName()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.FirstName.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationUser_ShouldInitializeWithEmptyLastName()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.LastName.Should().BeEmpty();
    }
}
