using AllowanceTracker.Api.V1;
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
}
