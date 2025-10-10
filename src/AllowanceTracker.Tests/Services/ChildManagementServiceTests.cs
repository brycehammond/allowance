using AllowanceTracker.Data;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Tests.Services;

public class ChildManagementServiceTests
{
    private readonly AllowanceContext _context;
    private readonly ChildManagementService _service;

    public ChildManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AllowanceContext(options);
        _service = new ChildManagementService(_context);
    }

    [Fact]
    public async Task GetChildAsync_AsParentInSameFamily_ReturnsChild()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var parent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "Test",
            Role = UserRole.Parent,
            FamilyId = family.Id
        };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChildAsync(child.Id, parent.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(child.Id);
        result.User.FirstName.Should().Be("Child");
    }

    [Fact]
    public async Task GetChildAsync_AsChildSelf_ReturnsChild()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.Add(childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChildAsync(child.Id, childUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(child.Id);
    }

    [Fact]
    public async Task GetChildAsync_AsParentInDifferentFamily_ReturnsNull()
    {
        // Arrange
        var family1 = new Family { Id = Guid.NewGuid(), Name = "Family 1" };
        var family2 = new Family { Id = Guid.NewGuid(), Name = "Family 2" };
        var parent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "Test",
            Role = UserRole.Parent,
            FamilyId = family1.Id
        };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family2.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family2.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.AddRange(family1, family2);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChildAsync(child.Id, parent.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChildAsync_AsChildOther_ReturnsNull()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var childUser1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child1@test.com",
            FirstName = "Child1",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var childUser2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child2@test.com",
            FirstName = "Child2",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child1 = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser1.Id,
            User = childUser1,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };
        var child2 = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser2.Id,
            User = childUser2,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.AddRange(childUser1, childUser2);
        _context.Children.AddRange(child1, child2);
        await _context.SaveChangesAsync();

        // Act - child2 trying to access child1's data
        var result = await _service.GetChildAsync(child1.Id, childUser2.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateChildAllowanceAsync_AsParent_UpdatesAllowance()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var parent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "Test",
            Role = UserRole.Parent,
            FamilyId = family.Id
        };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateChildAllowanceAsync(child.Id, 20m, parent.Id);

        // Assert
        result.Should().NotBeNull();
        result!.WeeklyAllowance.Should().Be(20m);

        var updated = await _context.Children.FindAsync(child.Id);
        updated!.WeeklyAllowance.Should().Be(20m);
    }

    [Fact]
    public async Task UpdateChildAllowanceAsync_AsChild_ReturnsNull()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.Add(childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act - child trying to update their own allowance
        var result = await _service.UpdateChildAllowanceAsync(child.Id, 100m, childUser.Id);

        // Assert
        result.Should().BeNull();

        var unchanged = await _context.Children.FindAsync(child.Id);
        unchanged!.WeeklyAllowance.Should().Be(10m); // Unchanged
    }

    [Fact]
    public async Task UpdateChildAllowanceAsync_AsParentDifferentFamily_ReturnsNull()
    {
        // Arrange
        var family1 = new Family { Id = Guid.NewGuid(), Name = "Family 1" };
        var family2 = new Family { Id = Guid.NewGuid(), Name = "Family 2" };
        var parent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "Test",
            Role = UserRole.Parent,
            FamilyId = family1.Id
        };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family2.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family2.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.AddRange(family1, family2);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateChildAllowanceAsync(child.Id, 20m, parent.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteChildAsync_AsParent_DeletesChildAndUser()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var parent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "Test",
            Role = UserRole.Parent,
            FamilyId = family.Id
        };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteChildAsync(child.Id, parent.Id);

        // Assert
        result.Should().BeTrue();

        var deletedChild = await _context.Children.FindAsync(child.Id);
        deletedChild.Should().BeNull();

        var deletedUser = await _context.Users.FindAsync(childUser.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteChildAsync_AsChild_ReturnsFalse()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.Add(childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act - child trying to delete themselves
        var result = await _service.DeleteChildAsync(child.Id, childUser.Id);

        // Assert
        result.Should().BeFalse();

        var stillExists = await _context.Children.FindAsync(child.Id);
        stillExists.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteChildAsync_AsParentDifferentFamily_ReturnsFalse()
    {
        // Arrange
        var family1 = new Family { Id = Guid.NewGuid(), Name = "Family 1" };
        var family2 = new Family { Id = Guid.NewGuid(), Name = "Family 2" };
        var parent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "Test",
            Role = UserRole.Parent,
            FamilyId = family1.Id
        };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family2.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family2.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.AddRange(family1, family2);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteChildAsync(child.Id, parent.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanUserAccessChildAsync_AsParentSameFamily_ReturnsTrue()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var parent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "Test",
            Role = UserRole.Parent,
            FamilyId = family.Id
        };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserAccessChildAsync(parent.Id, child.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserAccessChildAsync_AsChildSelf_ReturnsTrue()
    {
        // Arrange
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.Add(family);
        _context.Users.Add(childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserAccessChildAsync(childUser.Id, child.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserAccessChildAsync_Unauthorized_ReturnsFalse()
    {
        // Arrange
        var family1 = new Family { Id = Guid.NewGuid(), Name = "Family 1" };
        var family2 = new Family { Id = Guid.NewGuid(), Name = "Family 2" };
        var parent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "Test",
            Role = UserRole.Parent,
            FamilyId = family1.Id
        };
        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Child",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = family2.Id
        };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            FamilyId = family2.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m
        };

        _context.Families.AddRange(family1, family2);
        _context.Users.AddRange(parent, childUser);
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanUserAccessChildAsync(parent.Id, child.Id);

        // Assert
        result.Should().BeFalse();
    }
}
