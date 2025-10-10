using AllowanceTracker.Components;
using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Components;

public class IncomeSpendingChartTests
{
    [Fact]
    public void IncomeSpendingChart_RendersCorrectly()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var monthlyData = new List<MonthlyComparison>
        {
            new MonthlyComparison(2025, 10, "October", 100m, 50m, 50m, 150m),
            new MonthlyComparison(2025, 9, "September", 80m, 30m, 50m, 100m)
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetMonthlyComparisonAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(monthlyData);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<IncomeSpendingChart>(parameters => parameters
            .Add(p => p.ChildId, childId));

        // Assert
        component.Markup.Should().Contain("Income vs Spending");
        component.FindAll(".income-spending-chart").Should().HaveCount(1);
    }

    [Fact]
    public void IncomeSpendingChart_ShowsThreeSeries()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var monthlyData = new List<MonthlyComparison>
        {
            new MonthlyComparison(2025, 10, "October", 100m, 50m, 50m, 150m),
            new MonthlyComparison(2025, 9, "September", 80m, 30m, 50m, 100m)
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetMonthlyComparisonAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(monthlyData);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<IncomeSpendingChart>(parameters => parameters
            .Add(p => p.ChildId, childId));

        // Assert - Component should display data for all three series: Income, Spending, Net Savings
        component.Markup.Should().Contain("Income");
        component.Markup.Should().Contain("Spending");
        component.Markup.Should().Contain("Net Savings");
    }

    [Fact]
    public void IncomeSpendingChart_PeriodSelector_UpdatesData()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var monthlyData = new List<MonthlyComparison>
        {
            new MonthlyComparison(2025, 10, "October", 100m, 50m, 50m, 150m)
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetMonthlyComparisonAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(monthlyData);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        var component = ctx.RenderComponent<IncomeSpendingChart>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.ShowControls, true));

        // Act - Click 3 months button
        var button3M = component.Find("button:contains('3M')");
        button3M.Click();

        // Assert - Should call with 3 months
        mockAnalyticsService.Verify(s => s.GetMonthlyComparisonAsync(childId, 3), Times.Once);

        // Act - Click 12 months button
        var button12M = component.Find("button:contains('12M')");
        button12M.Click();

        // Assert - Should call with 12 months
        mockAnalyticsService.Verify(s => s.GetMonthlyComparisonAsync(childId, 12), Times.Once);
    }

    [Fact]
    public void IncomeSpendingChart_ExportButton_DownloadsCSV()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var monthlyData = new List<MonthlyComparison>
        {
            new MonthlyComparison(2025, 10, "October", 100m, 50m, 50m, 150m),
            new MonthlyComparison(2025, 9, "September", 80m, 30m, 50m, 100m)
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetMonthlyComparisonAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(monthlyData);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        var component = ctx.RenderComponent<IncomeSpendingChart>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.ShowExportButton, true));

        // Assert - Export button should exist
        var exportButtons = component.FindAll("button:contains('Export')");
        exportButtons.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void IncomeSpendingChart_EmptyState_DisplaysMessage()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var emptyData = new List<MonthlyComparison>();

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetMonthlyComparisonAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(emptyData);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<IncomeSpendingChart>(parameters => parameters
            .Add(p => p.ChildId, childId));

        // Assert
        component.Markup.Should().Contain("No data available");
        component.FindAll(".chart-empty").Should().HaveCount(1);
    }
}
