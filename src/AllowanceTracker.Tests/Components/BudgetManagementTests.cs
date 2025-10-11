using AllowanceTracker.Components;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Components;

public class BudgetManagementTests : TestContext
{
    private readonly Mock<ICategoryBudgetService> _mockBudgetService;
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly Guid _childId = Guid.NewGuid();

    public BudgetManagementTests()
    {
        _mockBudgetService = new Mock<ICategoryBudgetService>();
        _mockCategoryService = new Mock<ICategoryService>();

        Services.AddSingleton(_mockBudgetService.Object);
        Services.AddSingleton(_mockCategoryService.Object);
    }

    [Fact]
    public void BudgetManagement_RendersTitle()
    {
        // Arrange
        SetupDefaultMocks();

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        component.Markup.Should().Contain("Budget Management");
    }

    [Fact]
    public void BudgetManagement_DisplaysExistingBudgets()
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
                Period = BudgetPeriod.Weekly,
                EnforceLimit = true
            },
            new CategoryBudget
            {
                Id = Guid.NewGuid(),
                ChildId = _childId,
                Category = TransactionCategory.Games,
                Limit = 30m,
                Period = BudgetPeriod.Monthly,
                EnforceLimit = false
            }
        };

        _mockBudgetService
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(budgets);
        _mockCategoryService
            .Setup(x => x.GetCategoryDisplayName(It.IsAny<TransactionCategory>()))
            .Returns<TransactionCategory>(cat => cat.ToString());
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, It.IsAny<BudgetPeriod>()))
            .ReturnsAsync(new List<CategoryBudgetStatusDto>());

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        component.Markup.Should().Contain("Toys");
        component.Markup.Should().Contain("$50.00");
        component.Markup.Should().Contain("Games");
        component.Markup.Should().Contain("$30.00");
    }

    [Fact]
    public void BudgetManagement_DisplaysNoBudgetsMessage_WhenEmpty()
    {
        // Arrange
        _mockBudgetService
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(new List<CategoryBudget>());
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, It.IsAny<BudgetPeriod>()))
            .ReturnsAsync(new List<CategoryBudgetStatusDto>());

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        component.Markup.Should().Contain("No budgets set");
    }

    [Fact]
    public void BudgetManagement_ShowsAddBudgetButton()
    {
        // Arrange
        SetupDefaultMocks();

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        var addButton = component.Find("button");
        addButton.TextContent.Should().Contain("Add Budget");
    }

    [Fact]
    public void BudgetManagement_DisplaysBudgetPeriod()
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
                Period = BudgetPeriod.Weekly,
                EnforceLimit = true
            }
        };

        _mockBudgetService
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(budgets);
        _mockCategoryService
            .Setup(x => x.GetCategoryDisplayName(It.IsAny<TransactionCategory>()))
            .Returns<TransactionCategory>(cat => cat.ToString());
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, It.IsAny<BudgetPeriod>()))
            .ReturnsAsync(new List<CategoryBudgetStatusDto>());

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        component.Markup.Should().Contain("Weekly");
    }

    [Fact]
    public void BudgetManagement_DisplaysEnforceStatus()
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
                Period = BudgetPeriod.Weekly,
                EnforceLimit = true
            }
        };

        _mockBudgetService
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(budgets);
        _mockCategoryService
            .Setup(x => x.GetCategoryDisplayName(It.IsAny<TransactionCategory>()))
            .Returns<TransactionCategory>(cat => cat.ToString());
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, It.IsAny<BudgetPeriod>()))
            .ReturnsAsync(new List<CategoryBudgetStatusDto>());

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        component.Markup.Should().Contain("Enforced");
    }

    [Fact]
    public void BudgetManagement_ShowsEditButton_ForEachBudget()
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
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(budgets);
        _mockCategoryService
            .Setup(x => x.GetCategoryDisplayName(It.IsAny<TransactionCategory>()))
            .Returns<TransactionCategory>(cat => cat.ToString());
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, It.IsAny<BudgetPeriod>()))
            .ReturnsAsync(new List<CategoryBudgetStatusDto>());

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        var buttons = component.FindAll("button");
        buttons.Should().Contain(b => b.TextContent.Contains("Edit") || b.InnerHtml.Contains("Edit"));
    }

    [Fact]
    public void BudgetManagement_ShowsDeleteButton_ForEachBudget()
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
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(budgets);
        _mockCategoryService
            .Setup(x => x.GetCategoryDisplayName(It.IsAny<TransactionCategory>()))
            .Returns<TransactionCategory>(cat => cat.ToString());
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, It.IsAny<BudgetPeriod>()))
            .ReturnsAsync(new List<CategoryBudgetStatusDto>());

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        var buttons = component.FindAll("button");
        buttons.Should().Contain(b => b.TextContent.Contains("Delete") || b.InnerHtml.Contains("Delete"));
    }

    [Fact]
    public void BudgetManagement_DisplaysBudgetStatus()
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

        var budgetStatus = new List<CategoryBudgetStatusDto>
        {
            new CategoryBudgetStatusDto(
                TransactionCategory.Toys,
                "Toys",
                BudgetLimit: 50m,
                CurrentSpending: 30m,
                Remaining: 20m,
                PercentUsed: 60,
                Status: BudgetStatus.Warning,
                Period: BudgetPeriod.Weekly)
        };

        _mockBudgetService
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(budgets);
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, BudgetPeriod.Weekly))
            .ReturnsAsync(budgetStatus);
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, BudgetPeriod.Monthly))
            .ReturnsAsync(new List<CategoryBudgetStatusDto>());
        _mockCategoryService
            .Setup(x => x.GetCategoryDisplayName(It.IsAny<TransactionCategory>()))
            .Returns<TransactionCategory>(cat => cat.ToString());

        // Act
        var component = RenderComponent<BudgetManagement>(parameters => parameters
            .Add(p => p.ChildId, _childId));

        // Assert
        component.Markup.Should().Contain("$30.00"); // Spent amount
        component.Markup.Should().Contain("$20.00"); // Remaining amount
    }

    private void SetupDefaultMocks()
    {
        _mockBudgetService
            .Setup(x => x.GetAllBudgetsAsync(_childId))
            .ReturnsAsync(new List<CategoryBudget>());
        _mockCategoryService
            .Setup(x => x.GetCategoryDisplayName(It.IsAny<TransactionCategory>()))
            .Returns<TransactionCategory>(cat => cat.ToString());
        _mockCategoryService
            .Setup(x => x.GetBudgetStatusAsync(_childId, It.IsAny<BudgetPeriod>()))
            .ReturnsAsync(new List<CategoryBudgetStatusDto>());
    }
}
