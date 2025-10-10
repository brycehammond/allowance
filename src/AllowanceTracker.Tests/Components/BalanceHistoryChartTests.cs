using AllowanceTracker.Components;
using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Components;

public class BalanceHistoryChartTests
{
    [Fact]
    public void BalanceHistoryChart_RendersCorrectly()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var balanceHistory = new List<BalancePoint>
        {
            new BalancePoint(DateTime.UtcNow.AddDays(-2), 50.00m, "Weekly allowance"),
            new BalancePoint(DateTime.UtcNow.AddDays(-1), 45.00m, "Bought snack"),
            new BalancePoint(DateTime.UtcNow, 55.00m, "Chore payment")
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(balanceHistory);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<BalanceHistoryChart>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.DefaultDays, 30));

        // Assert
        component.Markup.Should().Contain("Balance History");
        component.FindAll(".balance-history-chart").Should().HaveCount(1);
    }

    [Fact]
    public void BalanceHistoryChart_LoadsDataOnInitialized()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var balanceHistory = new List<BalancePoint>
        {
            new BalancePoint(DateTime.UtcNow.AddDays(-30), 100.00m, "Starting balance"),
            new BalancePoint(DateTime.UtcNow, 150.00m, "Current balance")
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, 30))
            .ReturnsAsync(balanceHistory);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<BalanceHistoryChart>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.DefaultDays, 30));

        // Assert
        mockAnalyticsService.Verify(s => s.GetBalanceHistoryAsync(childId, 30), Times.Once);
    }

    [Fact]
    public void BalanceHistoryChart_DateRangeButtons_UpdateChart()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var balanceHistory = new List<BalancePoint>
        {
            new BalancePoint(DateTime.UtcNow, 100.00m, "Balance")
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(balanceHistory);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        var component = ctx.RenderComponent<BalanceHistoryChart>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.ShowControls, true));

        // Act - Click 7D button
        var button7D = component.Find("button:contains('7D')");
        button7D.Click();

        // Assert - Should call with 7 days
        mockAnalyticsService.Verify(s => s.GetBalanceHistoryAsync(childId, 7), Times.Once);

        // Act - Click 90D button
        var button90D = component.Find("button:contains('90D')");
        button90D.Click();

        // Assert - Should call with 90 days
        mockAnalyticsService.Verify(s => s.GetBalanceHistoryAsync(childId, 90), Times.Once);
    }

    [Fact]
    public void BalanceHistoryChart_EmptyData_ShowsEmptyState()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var emptyBalanceHistory = new List<BalancePoint>();

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(emptyBalanceHistory);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<BalanceHistoryChart>(parameters => parameters
            .Add(p => p.ChildId, childId));

        // Assert
        component.Markup.Should().Contain("No transaction history yet");
        component.FindAll(".chart-empty").Should().HaveCount(1);
    }

    [Fact]
    public void BalanceHistoryChart_Loading_ShowsSpinner()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<List<BalancePoint>>();

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .Returns(tcs.Task);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<BalanceHistoryChart>(parameters => parameters
            .Add(p => p.ChildId, childId));

        // Assert - Should show loading spinner while waiting
        component.FindAll(".spinner-border").Should().HaveCount(1);
        component.Markup.Should().Contain("chart-loading");

        // Complete the task
        tcs.SetResult(new List<BalancePoint>());
    }

    [Fact]
    public async Task BalanceHistoryChart_RealTimeUpdate_RefreshesData()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var initialBalance = new List<BalancePoint>
        {
            new BalancePoint(DateTime.UtcNow, 100.00m, "Initial")
        };
        var updatedBalance = new List<BalancePoint>
        {
            new BalancePoint(DateTime.UtcNow, 100.00m, "Initial"),
            new BalancePoint(DateTime.UtcNow, 110.00m, "New transaction")
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .SetupSequence(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(initialBalance)
            .ReturnsAsync(updatedBalance);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        var component = ctx.RenderComponent<BalanceHistoryChart>(parameters => parameters
            .Add(p => p.ChildId, childId));

        // Act - Simulate refresh
        await component.InvokeAsync(async () => await component.Instance.RefreshDataAsync());

        // Assert - Should call service twice (once on init, once on refresh)
        mockAnalyticsService.Verify(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()), Times.Exactly(2));
    }
}
