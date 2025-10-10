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

public class SavingsProgressWidgetTests
{
    [Fact]
    public void SavingsProgressWidget_RendersGauge()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var wishListItems = new List<WishListItem>
        {
            new WishListItem { Id = Guid.NewGuid(), Name = "Bike", Price = 100m, ChildId = childId }
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<SavingsProgressWidget>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.CurrentBalance, 50m)
            .Add(p => p.WeeklyAllowance, 10m)
            .Add(p => p.WishListItems, wishListItems));

        // Assert
        component.FindAll(".savings-progress-widget").Should().HaveCount(1);
        component.Markup.Should().Contain("$50.00");
    }

    [Fact]
    public void SavingsProgressWidget_DisplaysWishListItems()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var wishListItems = new List<WishListItem>
        {
            new WishListItem { Id = Guid.NewGuid(), Name = "Bike", Price = 100m, ChildId = childId },
            new WishListItem { Id = Guid.NewGuid(), Name = "Video Game", Price = 60m, ChildId = childId }
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<SavingsProgressWidget>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.CurrentBalance, 50m)
            .Add(p => p.WeeklyAllowance, 10m)
            .Add(p => p.WishListItems, wishListItems));

        // Assert
        component.Markup.Should().Contain("Bike");
        component.Markup.Should().Contain("Video Game");
        component.Markup.Should().Contain("$100.00");
        component.Markup.Should().Contain("$60.00");
    }

    [Fact]
    public void SavingsProgressWidget_CalculatesProgressPercent()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var wishListItems = new List<WishListItem>
        {
            new WishListItem { Id = Guid.NewGuid(), Name = "Toy", Price = 100m, ChildId = childId }
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act - Current balance is 75, item costs 100, so progress is 75%
        var component = ctx.RenderComponent<SavingsProgressWidget>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.CurrentBalance, 75m)
            .Add(p => p.WeeklyAllowance, 10m)
            .Add(p => p.WishListItems, wishListItems));

        // Assert
        component.Markup.Should().Contain("75%");
    }

    [Fact]
    public void SavingsProgressWidget_ShowsTimeToGoal()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var wishListItems = new List<WishListItem>
        {
            // Need $50 more, allowance is $10/week, so 5 weeks
            new WishListItem { Id = Guid.NewGuid(), Name = "Toy", Price = 100m, ChildId = childId }
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<SavingsProgressWidget>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.CurrentBalance, 50m)
            .Add(p => p.WeeklyAllowance, 10m)
            .Add(p => p.WishListItems, wishListItems));

        // Assert
        component.Markup.Should().Contain("5 weeks");
    }

    [Fact]
    public void SavingsProgressWidget_GoalReached_ShowsCelebration()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var wishListItems = new List<WishListItem>
        {
            new WishListItem { Id = Guid.NewGuid(), Name = "Toy", Price = 50m, ChildId = childId }
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act - Balance equals or exceeds price
        var component = ctx.RenderComponent<SavingsProgressWidget>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.CurrentBalance, 60m)
            .Add(p => p.WeeklyAllowance, 10m)
            .Add(p => p.WishListItems, wishListItems));

        // Assert
        component.Markup.Should().Contain("Goal reached");
    }

    [Fact]
    public void SavingsProgressWidget_EmptyWishList_ShowsCTA()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var emptyWishList = new List<WishListItem>();

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, It.IsAny<int>()))
            .ReturnsAsync(new List<BalancePoint>());

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<SavingsProgressWidget>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.CurrentBalance, 50m)
            .Add(p => p.WeeklyAllowance, 10m)
            .Add(p => p.WishListItems, emptyWishList));

        // Assert
        component.Markup.Should().Contain("No savings goals yet");
        component.FindAll(".empty-wish-list").Should().HaveCount(1);
    }

    [Fact]
    public void SavingsProgressWidget_Sparkline_DisplaysTrend()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var wishListItems = new List<WishListItem>
        {
            new WishListItem { Id = Guid.NewGuid(), Name = "Toy", Price = 100m, ChildId = childId }
        };

        var balanceTrend = new List<BalancePoint>
        {
            new BalancePoint(DateTime.UtcNow.AddDays(-7), 40m, "Starting"),
            new BalancePoint(DateTime.UtcNow.AddDays(-3), 45m, "Allowance"),
            new BalancePoint(DateTime.UtcNow, 50m, "Current")
        };

        var mockAnalyticsService = new Mock<ITransactionAnalyticsService>();
        mockAnalyticsService
            .Setup(s => s.GetBalanceHistoryAsync(childId, 7))
            .ReturnsAsync(balanceTrend);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockAnalyticsService.Object);

        // Act
        var component = ctx.RenderComponent<SavingsProgressWidget>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.CurrentBalance, 50m)
            .Add(p => p.WeeklyAllowance, 10m)
            .Add(p => p.WishListItems, wishListItems)
            .Add(p => p.ShowSparkline, true));

        // Assert
        component.Markup.Should().Contain("Last 7 Days");
        mockAnalyticsService.Verify(s => s.GetBalanceHistoryAsync(childId, 7), Times.Once);
    }
}
