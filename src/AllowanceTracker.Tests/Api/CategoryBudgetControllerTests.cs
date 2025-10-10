using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class CategoryBudgetControllerTests
{
    private readonly Mock<ICategoryBudgetService> _mockBudgetService;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly CategoryBudgetController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();

    public CategoryBudgetControllerTests()
    {
        _mockBudgetService = new Mock<ICategoryBudgetService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockCurrentUser.Setup(x => x.UserId).Returns(_userId);
        _controller = new CategoryBudgetController(_mockBudgetService.Object, _mockCurrentUser.Object);
    }

    [Fact]
    public async Task GetBudgets_ReturnsAllBudgetsForChild()
    {
        // Arrange
        var budgets = new List<CategoryBudget>
        {
            new CategoryBudget
            {
                Id = Guid.NewGuid(),
                ChildId = _childId,
                Category = TransactionCategory.Toys,
                Limit = 50m,
                Period = BudgetPeriod.Weekly
            }
        };
        _mockBudgetService
            .Setup(x => x.CanManageBudgetsAsync(_childId, _userId))
            .ReturnsAsync(true);
        _mockBudgetService
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(budgets);

        // Act
        var result = await _controller.GetBudgets(_childId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBudgets = okResult.Value.Should().BeAssignableTo<List<CategoryBudget>>().Subject;
        returnedBudgets.Should().HaveCount(1);
        returnedBudgets[0].Category.Should().Be(TransactionCategory.Toys);
    }

    [Fact]
    public async Task GetBudget_WithValidCategory_ReturnsBudget()
    {
        // Arrange
        var budget = new CategoryBudget
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Category = TransactionCategory.Toys,
            Limit = 50m,
            Period = BudgetPeriod.Weekly
        };
        _mockBudgetService
            .Setup(x => x.CanManageBudgetsAsync(_childId, _userId))
            .ReturnsAsync(true);
        _mockBudgetService
            .Setup(x => x.GetBudgetAsync(_childId, TransactionCategory.Toys))
            .ReturnsAsync(budget);

        // Act
        var result = await _controller.GetBudget(_childId, TransactionCategory.Toys);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBudget = okResult.Value.Should().BeAssignableTo<CategoryBudget>().Subject;
        returnedBudget.Category.Should().Be(TransactionCategory.Toys);
    }

    [Fact]
    public async Task GetBudget_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockBudgetService
            .Setup(x => x.CanManageBudgetsAsync(_childId, _userId))
            .ReturnsAsync(true);
        _mockBudgetService
            .Setup(x => x.GetBudgetAsync(_childId, TransactionCategory.Toys))
            .ReturnsAsync((CategoryBudget?)null);

        // Act
        var result = await _controller.GetBudget(_childId, TransactionCategory.Toys);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SetBudget_WithValidDto_CreatesBudget()
    {
        // Arrange
        var dto = new SetBudgetDto(_childId, TransactionCategory.Toys, 50m, BudgetPeriod.Weekly);
        var createdBudget = new CategoryBudget
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Category = TransactionCategory.Toys,
            Limit = 50m,
            Period = BudgetPeriod.Weekly
        };
        _mockBudgetService
            .Setup(x => x.SetBudgetAsync(dto, _userId))
            .ReturnsAsync(createdBudget);

        // Act
        var result = await _controller.SetBudget(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBudget = okResult.Value.Should().BeAssignableTo<CategoryBudget>().Subject;
        returnedBudget.Limit.Should().Be(50m);
    }

    [Fact]
    public async Task DeleteBudget_WithValidCategory_ReturnsNoContent()
    {
        // Arrange
        var budget = new CategoryBudget
        {
            Id = Guid.NewGuid(),
            ChildId = _childId,
            Category = TransactionCategory.Toys
        };
        _mockBudgetService
            .Setup(x => x.CanManageBudgetsAsync(_childId, _userId))
            .ReturnsAsync(true);
        _mockBudgetService
            .Setup(x => x.GetBudgetAsync(_childId, TransactionCategory.Toys))
            .ReturnsAsync(budget);
        _mockBudgetService
            .Setup(x => x.DeleteBudgetAsync(budget.Id, _userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteBudget(_childId, TransactionCategory.Toys);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteBudget_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockBudgetService
            .Setup(x => x.CanManageBudgetsAsync(_childId, _userId))
            .ReturnsAsync(true);
        _mockBudgetService
            .Setup(x => x.GetBudgetAsync(_childId, TransactionCategory.Toys))
            .ReturnsAsync((CategoryBudget?)null);

        // Act
        var result = await _controller.DeleteBudget(_childId, TransactionCategory.Toys);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SetBudget_WithUnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new SetBudgetDto(_childId, TransactionCategory.Toys, 50m, BudgetPeriod.Weekly);
        _mockBudgetService
            .Setup(x => x.SetBudgetAsync(dto, _userId))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _controller.SetBudget(dto));
    }
}
