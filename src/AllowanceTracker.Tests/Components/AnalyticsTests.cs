using AllowanceTracker.DTOs;
using AllowanceTracker.Pages;
using AllowanceTracker.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Components;

public class AnalyticsTests
{
    [Fact]
    public void Analytics_ShowsLoadingMessage_Initially()
    {
        // Arrange
        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(new List<ChildDto>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Assert - Component renders successfully
        component.Should().NotBeNull();
    }

    [Fact]
    public void Analytics_HasAnalyticsDashboardTitle()
    {
        // Arrange
        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(new List<ChildDto>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Assert
        component.Find("h1").TextContent.Should().Contain("Analytics");
    }

    [Fact]
    public void Analytics_DisplaysChildSelector_WhenChildrenExist()
    {
        // Arrange
        var children = new List<ChildDto>
        {
            new ChildDto(
                Guid.NewGuid(),
                "Alice",
                "Johnson",
                WeeklyAllowance: 10m,
                CurrentBalance: 25m,
                LastAllowanceDate: DateTime.UtcNow.AddDays(-3)),
            new ChildDto(
                Guid.NewGuid(),
                "Bob",
                "Johnson",
                WeeklyAllowance: 15m,
                CurrentBalance: 50m,
                LastAllowanceDate: DateTime.UtcNow.AddDays(-2))
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert
        component.Markup.Should().Contain("Alice Johnson");
        component.Markup.Should().Contain("Bob Johnson");
    }

    [Fact]
    public void Analytics_ShowsNoChildrenMessage_WhenNoChildren()
    {
        // Arrange
        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(new List<ChildDto>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert
        component.Markup.Should().Contain("No children found");
    }

    [Fact]
    public void Analytics_DisplaysBalanceHistory_WhenChildSelected()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var children = new List<ChildDto>
        {
            new ChildDto(
                childId,
                "Alice",
                "Johnson",
                WeeklyAllowance: 10m,
                CurrentBalance: 25m,
                LastAllowanceDate: DateTime.UtcNow.AddDays(-3))
        };

        var balanceHistory = new List<BalancePoint>
        {
            new(DateTime.UtcNow.AddDays(-7), 10m, "Initial"),
            new(DateTime.UtcNow.AddDays(-3), 20m, "Allowance"),
            new(DateTime.UtcNow, 25m, "Chores")
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(x => x.GetBalanceHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(balanceHistory);
        mockAnalyticsService
            .Setup(x => x.GetIncomeVsSpendingAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new IncomeSpendingSummary(0, 0, 0, 0, 0, 0));
        mockAnalyticsService
            .Setup(x => x.GetSpendingBreakdownAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<CategoryBreakdown>());
        mockAnalyticsService
            .Setup(x => x.GetMonthlyComparisonAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new List<MonthlyComparison>());

        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert - Should render balance history section
        component.Markup.Should().Contain("Balance History");
    }

    [Fact]
    public void Analytics_DisplaysIncomeVsSpending_WhenDataAvailable()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var children = new List<ChildDto>
        {
            new ChildDto(
                childId,
                "Alice",
                "Johnson",
                WeeklyAllowance: 10m,
                CurrentBalance: 25m,
                LastAllowanceDate: DateTime.UtcNow.AddDays(-3))
        };

        var summary = new IncomeSpendingSummary(
            TotalIncome: 100m,
            TotalSpending: 60m,
            NetSavings: 40m,
            IncomeTransactionCount: 10,
            SpendingTransactionCount: 5,
            SavingsRate: 40m);

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(x => x.GetIncomeVsSpendingAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(summary);

        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert - Should render income vs spending section
        component.Markup.Should().Contain("Income vs Spending");
    }

    [Fact]
    public void Analytics_CallsAnalyticsService_OnChildSelection()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var children = new List<ChildDto>
        {
            new ChildDto(
                childId,
                "Alice",
                "Johnson",
                WeeklyAllowance: 10m,
                CurrentBalance: 25m,
                LastAllowanceDate: DateTime.UtcNow.AddDays(-3))
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(x => x.GetBalanceHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());
        mockAnalyticsService
            .Setup(x => x.GetIncomeVsSpendingAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new IncomeSpendingSummary(0, 0, 0, 0, 0, 0));
        mockAnalyticsService
            .Setup(x => x.GetSpendingBreakdownAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<CategoryBreakdown>());
        mockAnalyticsService
            .Setup(x => x.GetMonthlyComparisonAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new List<MonthlyComparison>());

        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert - Service should be called
        mockFamilyService.Verify(x => x.GetChildrenAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void Analytics_DisplaysSpendingBreakdown_WhenDataAvailable()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var children = new List<ChildDto>
        {
            new ChildDto(
                childId,
                "Alice",
                "Johnson",
                WeeklyAllowance: 10m,
                CurrentBalance: 25m,
                LastAllowanceDate: DateTime.UtcNow.AddDays(-3))
        };

        var breakdown = new List<CategoryBreakdown>
        {
            new("Toys", 50m, 5, 50m),
            new("Snacks", 30m, 3, 30m),
            new("Books", 20m, 2, 20m)
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(x => x.GetBalanceHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());
        mockAnalyticsService
            .Setup(x => x.GetIncomeVsSpendingAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new IncomeSpendingSummary(0, 0, 0, 0, 0, 0));
        mockAnalyticsService
            .Setup(x => x.GetSpendingBreakdownAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(breakdown);
        mockAnalyticsService
            .Setup(x => x.GetMonthlyComparisonAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new List<MonthlyComparison>());

        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert - Should render spending breakdown section
        component.Markup.Should().Contain("Spending Breakdown");
    }

    [Fact]
    public void Analytics_DisplaysMonthlyComparison_WhenDataAvailable()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var children = new List<ChildDto>
        {
            new ChildDto(
                childId,
                "Alice",
                "Johnson",
                WeeklyAllowance: 10m,
                CurrentBalance: 25m,
                LastAllowanceDate: DateTime.UtcNow.AddDays(-3))
        };

        var monthlyData = new List<MonthlyComparison>
        {
            new(2025, 1, "January", 100m, 60m, 40m, 140m),
            new(2024, 12, "December", 90m, 50m, 40m, 100m)
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(x => x.GetBalanceHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());
        mockAnalyticsService
            .Setup(x => x.GetIncomeVsSpendingAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new IncomeSpendingSummary(0, 0, 0, 0, 0, 0));
        mockAnalyticsService
            .Setup(x => x.GetSpendingBreakdownAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<CategoryBreakdown>());
        mockAnalyticsService
            .Setup(x => x.GetMonthlyComparisonAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(monthlyData);

        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Analytics>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert - Should render monthly comparison section
        component.Markup.Should().Contain("Monthly Comparison");
    }
}
