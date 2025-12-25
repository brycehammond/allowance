using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs.Allowances;
using AllowanceTracker.DTOs.Tasks;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class AllowanceControllerTests
{
    private readonly Mock<IAllowanceService> _mockAllowanceService;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<IChildManagementService> _mockChildManagementService;
    private readonly ChildrenController _controller;

    public AllowanceControllerTests()
    {
        _mockAllowanceService = new Mock<IAllowanceService>();
        _mockAccountService = new Mock<IAccountService>();
        _mockChildManagementService = new Mock<IChildManagementService>();

        _controller = new ChildrenController(
            _mockChildManagementService.Object,
            _mockAccountService.Object,
            Mock.Of<IFamilyService>(),
            Mock.Of<ITransactionService>(),
            _mockAllowanceService.Object,
            Mock.Of<ITaskService>(),
            Mock.Of<ICurrentUserService>());
    }

    #region PauseAllowance Tests

    [Fact]
    public async Task PauseAllowance_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var child = new Child { Id = childId, FamilyId = Guid.NewGuid() };
        var dto = new PauseAllowanceDto("Going on vacation");

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync(child);
        _mockAllowanceService.Setup(x => x.PauseAllowanceAsync(childId, "Going on vacation")).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PauseAllowance(childId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockAllowanceService.Verify(x => x.PauseAllowanceAsync(childId, "Going on vacation"), Times.Once);
    }

    [Fact]
    public async Task PauseAllowance_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var dto = new PauseAllowanceDto("Reason");
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.PauseAllowance(childId, dto);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task PauseAllowance_WhenChildNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var dto = new PauseAllowanceDto("Reason");

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync((Child?)null);

        // Act
        var result = await _controller.PauseAllowance(childId, dto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region ResumeAllowance Tests

    [Fact]
    public async Task ResumeAllowance_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var child = new Child { Id = childId, FamilyId = Guid.NewGuid(), AllowancePaused = true };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync(child);
        _mockAllowanceService.Setup(x => x.ResumeAllowanceAsync(childId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResumeAllowance(childId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockAllowanceService.Verify(x => x.ResumeAllowanceAsync(childId), Times.Once);
    }

    [Fact]
    public async Task ResumeAllowance_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.ResumeAllowance(childId);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region AdjustAllowanceAmount Tests

    [Fact]
    public async Task AdjustAllowanceAmount_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var child = new Child { Id = childId, FamilyId = Guid.NewGuid(), WeeklyAllowance = 10m };
        var dto = new AdjustAllowanceAmountDto(20m, "Performance improvement");

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync(child);
        _mockAllowanceService.Setup(x => x.AdjustAllowanceAmountAsync(childId, 20m, "Performance improvement")).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AdjustAllowanceAmount(childId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockAllowanceService.Verify(x => x.AdjustAllowanceAmountAsync(childId, 20m, "Performance improvement"), Times.Once);
    }

    [Fact]
    public async Task AdjustAllowanceAmount_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var child = new Child { Id = childId, FamilyId = Guid.NewGuid() };
        var dto = new AdjustAllowanceAmountDto(-5m, "Invalid");

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync(child);
        _mockAllowanceService.Setup(x => x.AdjustAllowanceAmountAsync(childId, -5m, "Invalid"))
            .ThrowsAsync(new ArgumentException("Allowance amount cannot be negative"));

        // Act
        var result = await _controller.AdjustAllowanceAmount(childId, dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetAllowanceHistory Tests

    [Fact]
    public async Task GetAllowanceHistory_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var currentUser = new ApplicationUser { Id = userId, Role = UserRole.Parent };
        var child = new Child { Id = childId, FamilyId = Guid.NewGuid() };

        var history = new List<AllowanceAdjustmentDto>
        {
            new(Guid.NewGuid(), childId, AllowanceAdjustmentType.Paused, null, null, "Vacation", userId, "Parent User", DateTime.UtcNow.AddDays(-5)),
            new(Guid.NewGuid(), childId, AllowanceAdjustmentType.Resumed, null, null, null, userId, "Parent User", DateTime.UtcNow.AddDays(-2)),
            new(Guid.NewGuid(), childId, AllowanceAdjustmentType.AmountChanged, 10m, 15m, "Raise", userId, "Parent User", DateTime.UtcNow)
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, userId)).ReturnsAsync(child);
        _mockAllowanceService.Setup(x => x.GetAllowanceAdjustmentHistoryAsync(childId)).ReturnsAsync(history);

        // Act
        var result = await _controller.GetAllowanceHistory(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedHistory = okResult.Value.Should().BeAssignableTo<List<AllowanceAdjustmentDto>>().Subject;
        returnedHistory.Should().HaveCount(3);
        _mockAllowanceService.Verify(x => x.GetAllowanceAdjustmentHistoryAsync(childId), Times.Once);
    }

    [Fact]
    public async Task GetAllowanceHistory_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.GetAllowanceHistory(childId);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion
}
