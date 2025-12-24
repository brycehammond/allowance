using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class AnalyticsControllerTests
{
    private readonly Mock<ITransactionAnalyticsService> _mockAnalyticsService;
    private readonly AnalyticsController _controller;

    public AnalyticsControllerTests()
    {
        _mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        _controller = new AnalyticsController(_mockAnalyticsService.Object);
    }

    [Fact]
    public async Task GetBalanceHistory_ReturnsOkWithBalancePoints()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var balancePoints = new List<BalancePoint>
        {
            new(DateTime.UtcNow.AddDays(-2), 50m, "Allowance"),
            new(DateTime.UtcNow.AddDays(-1), 45m, "Spent on toys"),
            new(DateTime.UtcNow, 55m, "Chores bonus")
        };

        _mockAnalyticsService
            .Setup(x => x.GetBalanceHistoryAsync(childId, 30))
            .ReturnsAsync(balancePoints);

        // Act
        var result = await _controller.GetBalanceHistory(childId, 30);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var points = okResult.Value.Should().BeAssignableTo<List<BalancePoint>>().Subject;
        points.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetIncomeVsSpending_ReturnsOkWithSummary()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var summary = new IncomeSpendingSummary(
            TotalIncome: 100m,
            TotalSpending: 60m,
            NetSavings: 40m,
            IncomeTransactionCount: 10,
            SpendingTransactionCount: 5,
            SavingsRate: 40m);

        _mockAnalyticsService
            .Setup(x => x.GetIncomeVsSpendingAsync(childId, null, null))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetIncomeVsSpending(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSummary = okResult.Value.Should().BeAssignableTo<IncomeSpendingSummary>().Subject;
        returnedSummary.TotalIncome.Should().Be(100m);
        returnedSummary.SavingsRate.Should().Be(40m);
    }

    [Fact]
    public async Task GetSpendingTrend_ReturnsOkWithTrendData()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var trendData = new TrendData(
            Points: new List<DataPoint>
            {
                new(DateTime.UtcNow.AddDays(-7), 20m),
                new(DateTime.UtcNow, 25m)
            },
            Direction: TrendDirection.Up,
            ChangePercent: 25m,
            Description: "Spending increased by 25% this week");

        _mockAnalyticsService
            .Setup(x => x.GetSpendingTrendAsync(childId, TimePeriod.Week))
            .ReturnsAsync(trendData);

        // Act
        var result = await _controller.GetSpendingTrend(childId, TimePeriod.Week);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var trend = okResult.Value.Should().BeAssignableTo<TrendData>().Subject;
        trend.Direction.Should().Be(TrendDirection.Up);
        trend.ChangePercent.Should().Be(25m);
    }

    [Fact]
    public async Task GetSavingsRate_ReturnsOkWithRate()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var savingsRate = 45.5m;

        _mockAnalyticsService
            .Setup(x => x.GetSavingsRateAsync(childId, TimePeriod.Month))
            .ReturnsAsync(savingsRate);

        // Act
        var result = await _controller.GetSavingsRate(childId, TimePeriod.Month);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var rate = okResult.Value.Should().BeAssignableTo<object>().Subject;

        var rateProp = rate.GetType().GetProperty("savingsRate");
        rateProp.Should().NotBeNull();
        var actualRate = rateProp!.GetValue(rate);
        actualRate.Should().Be(45.5m);
    }

    [Fact]
    public async Task GetMonthlyComparison_ReturnsOkWithMonthlyData()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var monthlyData = new List<MonthlyComparison>
        {
            new(2025, 1, "January", 100m, 60m, 40m, 140m),
            new(2024, 12, "December", 90m, 50m, 40m, 100m)
        };

        _mockAnalyticsService
            .Setup(x => x.GetMonthlyComparisonAsync(childId, 6))
            .ReturnsAsync(monthlyData);

        // Act
        var result = await _controller.GetMonthlyComparison(childId, 6);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<List<MonthlyComparison>>().Subject;
        data.Should().HaveCount(2);
        data[0].MonthName.Should().Be("January");
    }

    [Fact]
    public async Task GetTransactionHeatmap_ReturnsOkWithHeatmapData()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var heatmapData = new Dictionary<DateTime, TransactionDayData>
        {
            { DateTime.UtcNow.Date, new TransactionDayData(5, 50m, 10m) },
            { DateTime.UtcNow.AddDays(-1).Date, new TransactionDayData(2, 20m, 5m) }
        };

        _mockAnalyticsService
            .Setup(x => x.GetTransactionHeatmapDataAsync(childId, 365))
            .ReturnsAsync(heatmapData);

        // Act
        var result = await _controller.GetTransactionHeatmap(childId, 365);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<Dictionary<DateTime, TransactionDayData>>().Subject;
        data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSpendingBreakdown_ReturnsOkWithCategoryData()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var breakdown = new List<CategoryBreakdown>
        {
            new("Toys", 50m, 5, 50m),
            new("Snacks", 30m, 3, 30m),
            new("Books", 20m, 2, 20m)
        };

        _mockAnalyticsService
            .Setup(x => x.GetSpendingBreakdownAsync(childId, null, null))
            .ReturnsAsync(breakdown);

        // Act
        var result = await _controller.GetSpendingBreakdown(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var categories = okResult.Value.Should().BeAssignableTo<List<CategoryBreakdown>>().Subject;
        categories.Should().HaveCount(3);
        categories[0].Category.Should().Be("Toys");
    }

    [Fact]
    public async Task GetBalanceHistory_UsesDefaultDays_WhenNotSpecified()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _mockAnalyticsService
            .Setup(x => x.GetBalanceHistoryAsync(childId, 30))
            .ReturnsAsync(new List<BalancePoint>());

        // Act
        await _controller.GetBalanceHistory(childId);

        // Assert
        _mockAnalyticsService.Verify(
            x => x.GetBalanceHistoryAsync(childId, 30),
            Times.Once);
    }

    [Fact]
    public async Task GetMonthlyComparison_UsesDefaultMonths_WhenNotSpecified()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _mockAnalyticsService
            .Setup(x => x.GetMonthlyComparisonAsync(childId, 6))
            .ReturnsAsync(new List<MonthlyComparison>());

        // Act
        await _controller.GetMonthlyComparison(childId);

        // Assert
        _mockAnalyticsService.Verify(
            x => x.GetMonthlyComparisonAsync(childId, 6),
            Times.Once);
    }

    [Fact]
    public async Task GetSavingsRate_UsesDefaultPeriod_WhenNotSpecified()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _mockAnalyticsService
            .Setup(x => x.GetSavingsRateAsync(childId, TimePeriod.Month))
            .ReturnsAsync(50m);

        // Act
        await _controller.GetSavingsRate(childId);

        // Assert
        _mockAnalyticsService.Verify(
            x => x.GetSavingsRateAsync(childId, TimePeriod.Month),
            Times.Once);
    }
}
