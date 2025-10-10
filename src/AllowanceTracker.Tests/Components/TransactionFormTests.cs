using AllowanceTracker.Components;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Components;

public class TransactionFormTests
{
    [Fact]
    public void TransactionForm_RendersWithRequiredFields()
    {
        // Arrange
        var mockTransactionService = new Mock<ITransactionService>();
        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockTransactionService.Object);

        // Act
        var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
            .Add(p => p.ChildId, Guid.NewGuid()));

        // Assert
        component.Find("input[name='amount']").Should().NotBeNull();
        component.Find("select[name='type']").Should().NotBeNull();
        component.Find("input[name='description']").Should().NotBeNull();
        component.Find("button[type='submit']").Should().NotBeNull();
    }

    [Fact]
    public void TransactionForm_HasCreditAndDebitOptions()
    {
        // Arrange
        var mockTransactionService = new Mock<ITransactionService>();
        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockTransactionService.Object);

        // Act
        var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
            .Add(p => p.ChildId, Guid.NewGuid()));

        // Assert
        var select = component.Find("select[name='type']");
        component.Markup.Should().Contain("Add Money (Credit)");
        component.Markup.Should().Contain("Spend Money (Debit)");
    }

    [Fact]
    public void TransactionForm_DefaultsToCredit()
    {
        // Arrange
        var mockTransactionService = new Mock<ITransactionService>();
        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockTransactionService.Object);

        // Act
        var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
            .Add(p => p.ChildId, Guid.NewGuid()));

        // Assert
        var select = component.Find("select[name='type']");
        select.GetAttribute("value").Should().Be(TransactionType.Credit.ToString());
    }

    [Fact]
    public void TransactionForm_HasCancelButton()
    {
        // Arrange
        var mockTransactionService = new Mock<ITransactionService>();
        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockTransactionService.Object);

        // Act
        var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
            .Add(p => p.ChildId, Guid.NewGuid()));

        // Assert
        var cancelButton = component.Find("button[type='button']");
        cancelButton.TextContent.Should().Contain("Cancel");
    }

    [Fact]
    public async Task TransactionForm_Submit_CallsTransactionService()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateTransactionAsync(It.IsAny<CreateTransactionDto>()))
            .ReturnsAsync(new Transaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = 25.00m,
                Type = TransactionType.Credit,
                Description = "Test transaction",
                BalanceAfter = 25.00m,
                CreatedById = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            });

        var onSavedCalled = false;

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockTransactionService.Object);

        var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
            .Add(p => p.ChildId, childId)
            .Add(p => p.OnSaved, EventCallback.Factory.Create(this, () => onSavedCalled = true)));

        // Act
        component.Find("input[name='amount']").Change("25.00");
        component.Find("input[name='description']").Change("Test transaction");
        component.Find("form").Submit();

        // Wait for async operation
        await Task.Delay(100);

        // Assert
        mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.Is<CreateTransactionDto>(
                dto => dto.ChildId == childId &&
                       dto.Amount == 25.00m &&
                       dto.Type == TransactionType.Credit &&
                       dto.Description == "Test transaction")),
            Times.Once);

        onSavedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionForm_Cancel_CallsOnCancelled()
    {
        // Arrange
        var mockTransactionService = new Mock<ITransactionService>();
        var onCancelledCalled = false;

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockTransactionService.Object);

        var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
            .Add(p => p.ChildId, Guid.NewGuid())
            .Add(p => p.OnCancelled, EventCallback.Factory.Create(this, () => onCancelledCalled = true)));

        // Act
        var cancelButton = component.Find("button[type='button']");
        cancelButton.Click();

        // Wait for async operation
        await Task.Delay(50);

        // Assert
        onCancelledCalled.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionForm_DisablesSaveButton_WhileSaving()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var mockTransactionService = new Mock<ITransactionService>();

        // Make the service delay to simulate saving
        var tcs = new TaskCompletionSource<Transaction>();
        mockTransactionService
            .Setup(x => x.CreateTransactionAsync(It.IsAny<CreateTransactionDto>()))
            .Returns(tcs.Task);

        using var ctx = new TestContext();
        ctx.Services.AddSingleton(mockTransactionService.Object);

        var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
            .Add(p => p.ChildId, childId));

        // Act
        component.Find("input[name='amount']").Change("10.00");
        component.Find("input[name='description']").Change("Test");

        var submitButton = component.Find("button[type='submit']");
        submitButton.Click();

        // Assert - Button should be disabled while saving
        await Task.Delay(50);
        submitButton.HasAttribute("disabled").Should().BeTrue();
        submitButton.TextContent.Should().Contain("Saving...");

        // Complete the save
        tcs.SetResult(new Transaction());
    }
}
