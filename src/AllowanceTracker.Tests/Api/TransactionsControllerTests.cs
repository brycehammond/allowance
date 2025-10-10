using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class TransactionsControllerTests
{
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _mockTransactionService = new Mock<ITransactionService>();
        _controller = new TransactionsController(_mockTransactionService.Object);
    }

    [Fact]
    public async Task GetChildTransactions_ReturnsOkWithTransactions()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), ChildId = childId, Amount = 10m, Type = TransactionType.Credit },
            new() { Id = Guid.NewGuid(), ChildId = childId, Amount = 5m, Type = TransactionType.Debit }
        };

        _mockTransactionService
            .Setup(x => x.GetChildTransactionsAsync(childId, 20))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetChildTransactions(childId, 20);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTransactions = okResult.Value.Should().BeAssignableTo<List<Transaction>>().Subject;
        returnedTransactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetChildTransactions_UsesDefaultLimit()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var transactions = new List<Transaction>();

        _mockTransactionService
            .Setup(x => x.GetChildTransactionsAsync(childId, 20))
            .ReturnsAsync(transactions);

        // Act
        await _controller.GetChildTransactions(childId);

        // Assert
        _mockTransactionService.Verify(
            x => x.GetChildTransactionsAsync(childId, 20),
            Times.Once);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsCreatedWithTransaction()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var dto = new CreateTransactionDto(childId, 25m, TransactionType.Credit, "Allowance");
        var createdTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Amount = 25m,
            Type = TransactionType.Credit,
            Description = "Allowance",
            BalanceAfter = 25m,
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionService
            .Setup(x => x.CreateTransactionAsync(dto))
            .ReturnsAsync(createdTransaction);

        // Act
        var result = await _controller.CreateTransaction(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(TransactionsController.GetChildTransactions));
        createdResult.RouteValues!["childId"].Should().Be(childId);

        var returnedTransaction = createdResult.Value.Should().BeAssignableTo<Transaction>().Subject;
        returnedTransaction.Id.Should().Be(createdTransaction.Id);
        returnedTransaction.Amount.Should().Be(25m);
    }

    [Fact]
    public async Task GetBalance_ReturnsOkWithBalance()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var balance = 150.75m;

        _mockTransactionService
            .Setup(x => x.GetCurrentBalanceAsync(childId))
            .ReturnsAsync(balance);

        // Act
        var result = await _controller.GetBalance(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var balanceResponse = okResult.Value.Should().BeAssignableTo<object>().Subject;

        // Check the anonymous type has balance property
        var balanceProp = balanceResponse.GetType().GetProperty("balance");
        balanceProp.Should().NotBeNull();
        var actualBalance = balanceProp!.GetValue(balanceResponse);
        actualBalance.Should().Be(150.75m);
    }

    [Fact]
    public async Task CreateTransaction_CallsServiceWithCorrectDto()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var dto = new CreateTransactionDto(childId, 10m, TransactionType.Debit, "Spent on toys");
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Amount = 10m,
            Type = TransactionType.Debit
        };

        _mockTransactionService
            .Setup(x => x.CreateTransactionAsync(It.IsAny<CreateTransactionDto>()))
            .ReturnsAsync(transaction);

        // Act
        await _controller.CreateTransaction(dto);

        // Assert
        _mockTransactionService.Verify(
            x => x.CreateTransactionAsync(It.Is<CreateTransactionDto>(
                d => d.ChildId == childId &&
                     d.Amount == 10m &&
                     d.Type == TransactionType.Debit &&
                     d.Description == "Spent on toys")),
            Times.Once);
    }
}
