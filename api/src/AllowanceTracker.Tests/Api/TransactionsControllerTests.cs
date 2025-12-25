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
    public async Task CreateTransaction_ReturnsCreatedWithTransaction()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var dto = new CreateTransactionDto(childId, 25m, TransactionType.Credit, TransactionCategory.Allowance, "Allowance");
        var createdBy = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Parent",
            Email = "parent@test.com",
            UserName = "parent@test.com"
        };

        var createdTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Amount = 25m,
            Type = TransactionType.Credit,
            Category = TransactionCategory.Allowance,
            Description = "Allowance",
            BalanceAfter = 25m,
            CreatedById = createdBy.Id,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionService
            .Setup(x => x.CreateTransactionAsync(dto))
            .ReturnsAsync(createdTransaction);

        // Mock the reload call to get CreatedBy navigation property
        _mockTransactionService
            .Setup(x => x.GetChildTransactionsAsync(childId, 1))
            .ReturnsAsync(new List<Transaction> { createdTransaction });

        // Act
        var result = await _controller.CreateTransaction(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be("GetChildTransactions");
        createdResult.ControllerName.Should().Be("Children");
        createdResult.RouteValues!["childId"].Should().Be(childId);

        var returnedTransaction = createdResult.Value.Should().BeAssignableTo<TransactionDto>().Subject;
        returnedTransaction.Id.Should().Be(createdTransaction.Id);
        returnedTransaction.Amount.Should().Be(25m);
    }

    [Fact]
    public async Task CreateTransaction_CallsServiceWithCorrectDto()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var dto = new CreateTransactionDto(childId, 10m, TransactionType.Debit, TransactionCategory.Toys, "Spent on toys");
        var createdBy = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Parent",
            Email = "parent@test.com",
            UserName = "parent@test.com"
        };

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Amount = 10m,
            Type = TransactionType.Debit,
            Category = TransactionCategory.Toys,
            Description = "Spent on toys",
            BalanceAfter = 90m,
            CreatedById = createdBy.Id,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionService
            .Setup(x => x.CreateTransactionAsync(It.IsAny<CreateTransactionDto>()))
            .ReturnsAsync(transaction);

        // Mock the reload call to get CreatedBy navigation property
        _mockTransactionService
            .Setup(x => x.GetChildTransactionsAsync(childId, 1))
            .ReturnsAsync(new List<Transaction> { transaction });

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
