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
    private readonly Mock<IFamilyService> _mockFamilyService;
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly ChildrenController _controller;

    public ChildrenControllerTests()
    {
        _mockChildManagementService = new Mock<IChildManagementService>();
        _mockAccountService = new Mock<IAccountService>();
        _mockFamilyService = new Mock<IFamilyService>();
        _mockTransactionService = new Mock<ITransactionService>();
        _controller = new ChildrenController(
            _mockChildManagementService.Object,
            _mockAccountService.Object,
            _mockFamilyService.Object,
            _mockTransactionService.Object,
            Mock.Of<IAllowanceService>());
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

    // iOS Parity Tests

    [Fact]
    public async Task GetChildren_WithAuthenticatedUser_ReturnsListOfChildren()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var currentUser = new ApplicationUser
        {
            Id = userId,
            Email = "parent@test.com",
            Role = UserRole.Parent,
            FamilyId = familyId
        };

        var familyChildren = new FamilyChildrenDto(
            familyId,
            "Test Family",
            new List<ChildDetailDto>
            {
                new ChildDetailDto(Guid.NewGuid(), Guid.NewGuid(), "Alice", "Smith", "alice@test.com", 100m, 0m, 10m, null, null, DayOfWeek.Friday),
                new ChildDetailDto(Guid.NewGuid(), Guid.NewGuid(), "Bob", "Smith", "bob@test.com", 150m, 0m, 15m, DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(4), null)
            });

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockFamilyService.Setup(x => x.GetFamilyChildrenAsync(userId)).ReturnsAsync(familyChildren);

        // Act
        var result = await _controller.GetChildren();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var children = okResult.Value.Should().BeAssignableTo<List<ChildDto>>().Subject;
        children.Should().HaveCount(2);
        children[0].FirstName.Should().Be("Alice");
        children[1].FirstName.Should().Be("Bob");
    }

    [Fact]
    public async Task GetChildren_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.GetChildren();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetChildren_WhenUserHasNoFamily_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockFamilyService.Setup(x => x.GetFamilyChildrenAsync(userId)).ReturnsAsync((FamilyChildrenDto?)null);

        // Act
        var result = await _controller.GetChildren();

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetChildTransactions_WithValidChildId_ReturnsTransactions()
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

        var child = new Child
        {
            Id = childId,
            User = new ApplicationUser { FirstName = "Alice", LastName = "Smith", Email = "alice@test.com" }
        };

        var transactions = new List<Transaction>
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = 10m,
                Type = TransactionType.Credit,
                Category = TransactionCategory.Allowance,
                Description = "Weekly allowance",
                BalanceAfter = 100m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = new ApplicationUser { FirstName = "Parent", LastName = "User" }
            }
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync(child);
        _mockTransactionService.Setup(x => x.GetChildTransactionsAsync(childId, 20)).ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetChildTransactions(childId, 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var transactionDtos = okResult.Value.Should().BeAssignableTo<List<TransactionDto>>().Subject;
        transactionDtos.Should().HaveCount(1);
        transactionDtos[0].Description.Should().Be("Weekly allowance");
        transactionDtos[0].CreatedByName.Should().Be("Parent User");
    }

    [Fact]
    public async Task GetChildTransactions_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.GetChildTransactions(childId, 20);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetChildTransactions_WhenChildNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync((Child?)null);

        // Act
        var result = await _controller.GetChildTransactions(childId, 20);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    // UpdateChildSettings Tests

    [Fact]
    public async Task UpdateChildSettings_WithValidData_ReturnsOkWithUpdatedChild()
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

        var dto = new UpdateChildSettingsDto(
            WeeklyAllowance: 20m,
            SavingsAccountEnabled: true,
            SavingsTransferType: SavingsTransferType.Percentage,
            SavingsTransferPercentage: 25,
            SavingsTransferAmount: null,
            AllowanceDay: DayOfWeek.Friday);

        var updatedChild = new Child
        {
            Id = childId,
            WeeklyAllowance = 20m,
            AllowanceDay = DayOfWeek.Friday,
            SavingsAccountEnabled = true,
            SavingsTransferType = SavingsTransferType.Percentage,
            SavingsTransferPercentage = 25,
            User = new ApplicationUser { FirstName = "Alice", LastName = "Smith" }
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService
            .Setup(x => x.UpdateChildSettingsAsync(childId, dto, userId))
            .ReturnsAsync(updatedChild);

        // Act
        var result = await _controller.UpdateChildSettings(childId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateChildSettings_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var dto = new UpdateChildSettingsDto(
            WeeklyAllowance: 20m,
            AllowanceDay: DayOfWeek.Monday);

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.UpdateChildSettings(childId, dto);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateChildSettings_WhenChildNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var dto = new UpdateChildSettingsDto(
            WeeklyAllowance: 20m,
            AllowanceDay: DayOfWeek.Friday);

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService
            .Setup(x => x.UpdateChildSettingsAsync(childId, dto, userId))
            .ReturnsAsync((Child?)null);

        // Act
        var result = await _controller.UpdateChildSettings(childId, dto);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateChildSettings_WithAllowanceDay_UpdatesCorrectly()
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

        var dto = new UpdateChildSettingsDto(
            WeeklyAllowance: 15m,
            AllowanceDay: DayOfWeek.Sunday);

        var updatedChild = new Child
        {
            Id = childId,
            WeeklyAllowance = 15m,
            AllowanceDay = DayOfWeek.Sunday,
            User = new ApplicationUser { FirstName = "Bob", LastName = "Test" }
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService
            .Setup(x => x.UpdateChildSettingsAsync(childId, dto, userId))
            .ReturnsAsync(updatedChild);

        // Act
        var result = await _controller.UpdateChildSettings(childId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockChildManagementService.Verify(
            x => x.UpdateChildSettingsAsync(childId, dto, userId),
            Times.Once);
    }

    [Fact]
    public async Task UpdateChildSettings_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var dto = new UpdateChildSettingsDto(
            WeeklyAllowance: 30m,
            SavingsAccountEnabled: false,
            AllowanceDay: null); // Test null AllowanceDay

        var updatedChild = new Child
        {
            Id = childId,
            WeeklyAllowance = 30m,
            AllowanceDay = null,
            User = new ApplicationUser { FirstName = "Charlie", LastName = "Test" }
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService
            .Setup(x => x.UpdateChildSettingsAsync(It.IsAny<Guid>(), It.IsAny<UpdateChildSettingsDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(updatedChild);

        // Act
        await _controller.UpdateChildSettings(childId, dto);

        // Assert
        _mockChildManagementService.Verify(
            x => x.UpdateChildSettingsAsync(childId, dto, userId),
            Times.Once);
    }
}
