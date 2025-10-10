using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class ChildrenControllerTests
{
    private readonly Mock<IChildManagementService> _mockChildManagementService;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly ChildrenController _controller;

    public ChildrenControllerTests()
    {
        _mockChildManagementService = new Mock<IChildManagementService>();
        _mockAccountService = new Mock<IAccountService>();
        _controller = new ChildrenController(_mockChildManagementService.Object, _mockAccountService.Object);
    }

    [Fact]
    public async Task GetChild_WithValidChildId_ReturnsOkWithChildData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser
        {
            Id = userId,
            Email = "parent@test.com",
            FirstName = "Parent",
            LastName = "User",
            Role = UserRole.Parent
        };

        var child = new Child
        {
            Id = childId,
            UserId = Guid.NewGuid(),
            User = new ApplicationUser
            {
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice@test.com"
            },
            CurrentBalance = 100m,
            WeeklyAllowance = 10m,
            LastAllowanceDate = DateTime.UtcNow.AddDays(-3),
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync(child);

        // Act
        var result = await _controller.GetChild(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetChild_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.GetChild(childId);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetChild_WhenChildNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync((Child?)null);

        // Act
        var result = await _controller.GetChild(childId);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAllowance_WithValidData_ReturnsOkWithUpdatedChild()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser
        {
            Id = userId,
            Email = "parent@test.com",
            Role = UserRole.Parent
        };

        var dto = new UpdateAllowanceDto(15m);
        var updatedChild = new Child
        {
            Id = childId,
            WeeklyAllowance = 15m
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService
            .Setup(x => x.UpdateChildAllowanceAsync(childId, 15m, userId))
            .ReturnsAsync(updatedChild);

        // Act
        var result = await _controller.UpdateAllowance(childId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAllowance_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var dto = new UpdateAllowanceDto(15m);
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.UpdateAllowance(childId, dto);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateAllowance_WhenChildNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var dto = new UpdateAllowanceDto(15m);

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService
            .Setup(x => x.UpdateChildAllowanceAsync(childId, 15m, userId))
            .ReturnsAsync((Child?)null);

        // Act
        var result = await _controller.UpdateAllowance(childId, dto);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteChild_WithValidChildId_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser
        {
            Id = userId,
            Email = "parent@test.com",
            Role = UserRole.Parent
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.DeleteChildAsync(childId, userId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteChild(childId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteChild_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.DeleteChild(childId);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task DeleteChild_WhenChildNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.DeleteChildAsync(childId, userId)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteChild(childId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetChild_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId };
        var child = new Child
        {
            Id = childId,
            User = new ApplicationUser { FirstName = "Test", LastName = "Child", Email = "test@test.com" }
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(child);

        // Act
        await _controller.GetChild(childId);

        // Assert
        _mockChildManagementService.Verify(
            x => x.GetChildAsync(childId, userId),
            Times.Once);
    }
}
