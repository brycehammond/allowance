using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        _mockCategoryService = new Mock<ICategoryService>();
        _controller = new CategoryController(_mockCategoryService.Object);
    }

    [Fact]
    public void GetCategories_WithCreditType_ReturnsCreditCategories()
    {
        // Arrange
        var expectedCategories = new List<TransactionCategory>
        {
            TransactionCategory.Allowance,
            TransactionCategory.Gift,
            TransactionCategory.OtherIncome
        };
        _mockCategoryService
            .Setup(x => x.GetCategoriesForType(TransactionType.Credit))
            .Returns(expectedCategories);

        // Act
        var result = _controller.GetCategories(TransactionType.Credit);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var categories = okResult.Value.Should().BeAssignableTo<List<TransactionCategory>>().Subject;
        categories.Should().BeEquivalentTo(expectedCategories);
    }

    [Fact]
    public void GetCategories_WithDebitType_ReturnsDebitCategories()
    {
        // Arrange
        var expectedCategories = new List<TransactionCategory>
        {
            TransactionCategory.Toys,
            TransactionCategory.Candy,
            TransactionCategory.Savings
        };
        _mockCategoryService
            .Setup(x => x.GetCategoriesForType(TransactionType.Debit))
            .Returns(expectedCategories);

        // Act
        var result = _controller.GetCategories(TransactionType.Debit);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var categories = okResult.Value.Should().BeAssignableTo<List<TransactionCategory>>().Subject;
        categories.Should().BeEquivalentTo(expectedCategories);
    }

    [Fact]
    public void GetAllCategories_ReturnsAllCategories()
    {
        // Arrange
        var expectedCategories = Enum.GetValues<TransactionCategory>().ToList();
        _mockCategoryService
            .Setup(x => x.GetAllCategories())
            .Returns(expectedCategories);

        // Act
        var result = _controller.GetAllCategories();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var categories = okResult.Value.Should().BeAssignableTo<List<TransactionCategory>>().Subject;
        categories.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void GetCategoryDisplayName_ReturnsFormattedName()
    {
        // Arrange
        var category = TransactionCategory.OtherIncome;
        _mockCategoryService
            .Setup(x => x.GetCategoryDisplayName(category))
            .Returns("Other Income");

        // Act
        var result = _controller.GetCategoryDisplayName(category);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var displayName = okResult.Value.Should().BeOfType<string>().Subject;
        displayName.Should().Be("Other Income");
    }

    [Fact]
    public void GetCategories_CallsServiceWithCorrectType()
    {
        // Arrange
        var transactionType = TransactionType.Credit;

        // Act
        _controller.GetCategories(transactionType);

        // Assert
        _mockCategoryService.Verify(
            x => x.GetCategoriesForType(transactionType),
            Times.Once);
    }

    [Fact]
    public async Task GetCategorySpending_ReturnsSpendingBreakdown()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var expectedSpending = new List<CategorySpendingDto>
        {
            new(TransactionCategory.Toys, "Toys", 50m, 3, 50m),
            new(TransactionCategory.Games, "Games", 30m, 2, 30m),
            new(TransactionCategory.Snacks, "Snacks", 20m, 4, 20m)
        };
        _mockCategoryService
            .Setup(x => x.GetCategorySpendingAsync(childId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(expectedSpending);

        // Act
        var result = await _controller.GetCategorySpending(childId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var spending = okResult.Value.Should().BeAssignableTo<List<CategorySpendingDto>>().Subject;
        spending.Should().HaveCount(3);
        spending[0].Category.Should().Be(TransactionCategory.Toys);
    }

    [Fact]
    public async Task GetCategorySpending_WithDateRange_PassesDatesToService()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        _mockCategoryService
            .Setup(x => x.GetCategorySpendingAsync(childId, startDate, endDate))
            .ReturnsAsync(new List<CategorySpendingDto>());

        // Act
        await _controller.GetCategorySpending(childId, startDate, endDate);

        // Assert
        _mockCategoryService.Verify(
            x => x.GetCategorySpendingAsync(childId, startDate, endDate),
            Times.Once);
    }

    [Fact]
    public async Task GetBudgetStatus_ReturnsBudgetStatuses()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var expectedStatuses = new List<CategoryBudgetStatusDto>
        {
            new(TransactionCategory.Toys, "Toys", 100m, 50m, 50m, 50, BudgetStatus.Safe, BudgetPeriod.Weekly),
            new(TransactionCategory.Candy, "Candy", 20m, 18m, 2m, 90, BudgetStatus.Warning, BudgetPeriod.Weekly)
        };
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(childId, BudgetPeriod.Weekly))
            .ReturnsAsync(expectedStatuses);

        // Act
        var result = await _controller.GetBudgetStatus(childId, BudgetPeriod.Weekly);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statuses = okResult.Value.Should().BeAssignableTo<List<CategoryBudgetStatusDto>>().Subject;
        statuses.Should().HaveCount(2);
        statuses[0].Status.Should().Be(BudgetStatus.Safe);
        statuses[1].Status.Should().Be(BudgetStatus.Warning);
    }

    [Fact]
    public void SuggestCategory_ReturnsSuggestedCategory()
    {
        // Arrange
        var description = "Bought a toy car";
        var transactionType = TransactionType.Debit;
        _mockCategoryService
            .Setup(x => x.SuggestCategory(description, transactionType))
            .Returns(TransactionCategory.Toys);

        // Act
        var result = _controller.SuggestCategory(description, transactionType);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;
        response.Should().NotBeNull();
    }

    [Fact]
    public void SuggestCategory_WithCreditDescription_ReturnsIncomeCategory()
    {
        // Arrange
        var description = "Weekly allowance";
        var transactionType = TransactionType.Credit;
        _mockCategoryService
            .Setup(x => x.SuggestCategory(description, transactionType))
            .Returns(TransactionCategory.Allowance);

        // Act
        var result = _controller.SuggestCategory(description, transactionType);

        // Assert
        _mockCategoryService.Verify(
            x => x.SuggestCategory(description, transactionType),
            Times.Once);
    }
}
