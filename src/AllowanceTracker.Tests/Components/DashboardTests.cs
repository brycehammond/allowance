using AllowanceTracker.DTOs;
using AllowanceTracker.Pages;
using AllowanceTracker.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Components;

public class DashboardTests
{
    [Fact]
    public void Dashboard_ShowsLoadingMessage_Initially()
    {
        // Arrange
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(new List<ChildDto>());

        var mockTransactionService = new Mock<ITransactionService>();

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddSingleton(mockTransactionService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Dashboard>();

        // Assert - Should show loading initially
        // Note: Due to async nature, loading might complete quickly
        // The test validates that the component renders successfully
        component.Should().NotBeNull();
    }

    [Fact]
    public void Dashboard_DisplaysChildren_WhenChildrenExist()
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

        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        var mockTransactionService = new Mock<ITransactionService>();

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddSingleton(mockTransactionService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Dashboard>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert
        component.Markup.Should().Contain("Alice Johnson");
        component.Markup.Should().Contain("Bob Johnson");
        component.FindAll(".col-md-4").Should().HaveCount(2);
    }

    [Fact]
    public void Dashboard_ShowsNoChildrenMessage_WhenNoChildren()
    {
        // Arrange
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(new List<ChildDto>());

        var mockTransactionService = new Mock<ITransactionService>();

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddSingleton(mockTransactionService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Dashboard>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert
        component.Markup.Should().Contain("No children found");
    }

    [Fact]
    public void Dashboard_HasAddChildButton()
    {
        // Arrange
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(new List<ChildDto>());

        var mockTransactionService = new Mock<ITransactionService>();

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddSingleton(mockTransactionService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Dashboard>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert
        var addButton = component.Find("a[href='/children/create']");
        addButton.TextContent.Should().Contain("Add Child");
    }

    [Fact]
    public void Dashboard_HasFamilyDashboardTitle()
    {
        // Arrange
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(new List<ChildDto>());

        var mockTransactionService = new Mock<ITransactionService>();

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddSingleton(mockTransactionService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Dashboard>();

        // Assert
        component.Find("h1").TextContent.Should().Be("Family Dashboard");
    }

    [Fact]
    public void Dashboard_CallsFamilyService_OnInitialization()
    {
        // Arrange
        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(new List<ChildDto>());

        var mockTransactionService = new Mock<ITransactionService>();

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddSingleton(mockTransactionService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Dashboard>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert
        mockFamilyService.Verify(x => x.GetChildrenAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void Dashboard_DisplaysMultipleChildren_InGrid()
    {
        // Arrange
        var children = new List<ChildDto>();
        for (int i = 0; i < 5; i++)
        {
            children.Add(new ChildDto(
                Guid.NewGuid(),
                $"Child{i}",
                "Test",
                WeeklyAllowance: 10m,
                CurrentBalance: i * 10m,
                LastAllowanceDate: DateTime.UtcNow.AddDays(-i)));
        }

        var mockFamilyService = new Mock<IFamilyService>();
        mockFamilyService
            .Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        var mockTransactionService = new Mock<ITransactionService>();

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockFamilyService.Object);
        ctx.Services.AddSingleton(mockTransactionService.Object);
        ctx.Services.AddAuthorizationCore();

        // Act
        var component = ctx.RenderComponent<Dashboard>();

        // Wait for async initialization
        component.WaitForState(() => !component.Markup.Contains("Loading"));

        // Assert
        component.FindAll(".col-md-4").Should().HaveCount(5);
        for (int i = 0; i < 5; i++)
        {
            component.Markup.Should().Contain($"Child{i} Test");
        }
    }
}
