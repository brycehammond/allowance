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

public class ChildCardTests
{
    [Fact]
    public void ChildCard_DisplaysChildInformation()
    {
        // Arrange
        var child = new ChildDto(
            Guid.NewGuid(),
            "Jane",
            "Doe",
            WeeklyAllowance: 10.00m,
            CurrentBalance: 25.50m,
            LastAllowanceDate: DateTime.UtcNow.AddDays(-2));

        using var ctx = new TestContext();

        // Act
        var component = ctx.RenderComponent<ChildCard>(parameters => parameters
            .Add(p => p.Child, child));

        // Assert
        component.Find("h3").TextContent.Should().Contain("Jane Doe");
        component.Markup.Should().Contain("$25.50");
        component.Markup.Should().Contain("$10.00");
    }

    [Fact]
    public void ChildCard_ShowsLastAllowanceDate_WhenPresent()
    {
        // Arrange
        var lastPaid = new DateTime(2025, 10, 1);
        var child = new ChildDto(
            Guid.NewGuid(),
            "John",
            "Smith",
            WeeklyAllowance: 15.00m,
            CurrentBalance: 50.00m,
            LastAllowanceDate: lastPaid);

        using var ctx = new TestContext();

        // Act
        var component = ctx.RenderComponent<ChildCard>(parameters => parameters
            .Add(p => p.Child, child));

        // Assert
        component.Markup.Should().Contain("Last Paid:");
        component.Markup.Should().Contain("Oct 01, 2025");
    }

    [Fact]
    public void ChildCard_ShowsPendingMessage_WhenNoLastAllowanceDate()
    {
        // Arrange
        var child = new ChildDto(
            Guid.NewGuid(),
            "Emily",
            "Jones",
            WeeklyAllowance: 20.00m,
            CurrentBalance: 0.00m,
            LastAllowanceDate: null);

        using var ctx = new TestContext();

        // Act
        var component = ctx.RenderComponent<ChildCard>(parameters => parameters
            .Add(p => p.Child, child));

        // Assert
        component.Markup.Should().Contain("First allowance pending");
    }

    [Fact]
    public void ChildCard_ShowsNextAllowanceDate_WhenLastDatePresent()
    {
        // Arrange
        var lastPaid = new DateTime(2025, 10, 1);
        var child = new ChildDto(
            Guid.NewGuid(),
            "Mike",
            "Brown",
            WeeklyAllowance: 12.00m,
            CurrentBalance: 30.00m,
            LastAllowanceDate: lastPaid);

        using var ctx = new TestContext();

        // Act
        var component = ctx.RenderComponent<ChildCard>(parameters => parameters
            .Add(p => p.Child, child));

        // Assert
        component.Markup.Should().Contain("Next Payment:");
        component.Markup.Should().Contain("2025-10-08"); // 7 days later
    }

    [Fact]
    public void ChildCard_HasAddTransactionButton()
    {
        // Arrange
        var child = new ChildDto(
            Guid.NewGuid(),
            "Sarah",
            "Wilson",
            WeeklyAllowance: 8.00m,
            CurrentBalance: 15.00m,
            LastAllowanceDate: null);

        using var ctx = new TestContext();

        // Act
        var component = ctx.RenderComponent<ChildCard>(parameters => parameters
            .Add(p => p.Child, child));

        // Assert
        var button = component.Find("button");
        button.TextContent.Should().Contain("Add Transaction");
    }

    [Fact]
    public void ChildCard_ToggleTransactionForm_ShowsAndHidesForm()
    {
        // Arrange
        var child = new ChildDto(
            Guid.NewGuid(),
            "Alex",
            "Taylor",
            WeeklyAllowance: 10.00m,
            CurrentBalance: 20.00m,
            LastAllowanceDate: null);

        var mockTransactionService = new Mock<ITransactionService>();
        var mockCategoryService = new Mock<ICategoryService>();
        mockCategoryService
            .Setup(x => x.GetCategoriesForType(It.IsAny<TransactionType>()))
            .Returns(new List<TransactionCategory> { TransactionCategory.Allowance });

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockTransactionService.Object);
        ctx.Services.AddSingleton(mockCategoryService.Object);

        var component = ctx.RenderComponent<ChildCard>(parameters => parameters
            .Add(p => p.Child, child));

        // Act - Click to show form
        var button = component.Find("button:contains('Add Transaction')");
        button.Click();

        // Assert - Form should be visible (look for EditForm or Save Transaction button)
        component.FindAll("form").Should().HaveCount(1);
        component.Markup.Should().Contain("Save Transaction");

        // Act - Click Cancel button to hide form
        var cancelButton = component.Find("button:contains('Cancel')");
        cancelButton.Click();

        // Assert - Form should be hidden
        component.FindAll("form").Should().BeEmpty();
    }
}
