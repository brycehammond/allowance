using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class TransactionServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly ITransactionService _transactionService;
    private readonly Guid _currentUserId;

    public TransactionServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);

        // Setup mock current user
        _currentUserId = Guid.NewGuid();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);

        _transactionService = new TransactionService(_context, _mockCurrentUser.Object);
    }

    [Fact]
    public async Task CreateTransaction_Credit_IncreasesBalance()
    {
        // Arrange
        var child = await CreateTestChild(balance: 50.00m);
        var dto = new CreateTransactionDto(
            child.Id,
            25.00m,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            "Test credit");

        // Act
        var transaction = await _transactionService.CreateTransactionAsync(dto);

        // Assert
        transaction.BalanceAfter.Should().Be(75.00m);
        transaction.Amount.Should().Be(25.00m);
        transaction.Type.Should().Be(TransactionType.Credit);

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(75.00m);
    }

    [Fact]
    public async Task CreateTransaction_Debit_DecreasesBalance()
    {
        // Arrange
        var child = await CreateTestChild(balance: 50.00m);
        var dto = new CreateTransactionDto(
            child.Id,
            20.00m,
            TransactionType.Debit,
            TransactionCategory.OtherSpending,
            "Test debit");

        // Act
        var transaction = await _transactionService.CreateTransactionAsync(dto);

        // Assert
        transaction.BalanceAfter.Should().Be(30.00m);
        transaction.Amount.Should().Be(20.00m);
        transaction.Type.Should().Be(TransactionType.Debit);

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(30.00m);
    }

    [Fact]
    public async Task CreateTransaction_WithInsufficientFunds_ThrowsException()
    {
        // Arrange
        var child = await CreateTestChild(balance: 10.00m);
        var dto = new CreateTransactionDto(
            child.Id,
            25.00m,
            TransactionType.Debit,
            TransactionCategory.OtherSpending,
            "Test debit");

        // Act
        var act = () => _transactionService.CreateTransactionAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient funds");

        // Balance should remain unchanged
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(10.00m);
    }

    [Fact]
    public async Task CreateTransaction_WithNonExistentChild_ThrowsException()
    {
        // Arrange
        var dto = new CreateTransactionDto(
            Guid.NewGuid(),
            25.00m,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            "Test credit");

        // Act
        var act = () => _transactionService.CreateTransactionAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Child not found");
    }

    [Fact]
    public async Task CreateTransaction_RecordsCreatedBy()
    {
        // Arrange
        var child = await CreateTestChild(balance: 50.00m);
        var dto = new CreateTransactionDto(
            child.Id,
            25.00m,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            "Test credit");

        // Act
        var transaction = await _transactionService.CreateTransactionAsync(dto);

        // Assert
        transaction.CreatedById.Should().Be(_currentUserId);
    }

    [Fact]
    public async Task CreateTransaction_RecordsDescription()
    {
        // Arrange
        var child = await CreateTestChild(balance: 50.00m);
        var dto = new CreateTransactionDto(
            child.Id,
            25.00m,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            "Weekly allowance");

        // Act
        var transaction = await _transactionService.CreateTransactionAsync(dto);

        // Assert
        transaction.Description.Should().Be("Weekly allowance");
    }

    [Fact]
    public async Task CreateTransaction_SetsCreatedAtTimestamp()
    {
        // Arrange
        var child = await CreateTestChild(balance: 50.00m);
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var dto = new CreateTransactionDto(
            child.Id,
            25.00m,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            "Test credit");

        // Act
        var transaction = await _transactionService.CreateTransactionAsync(dto);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        transaction.CreatedAt.Should().BeAfter(beforeCreation);
        transaction.CreatedAt.Should().BeBefore(afterCreation);
    }

    [Fact]
    public async Task GetChildTransactions_ReturnsTransactionsInDescendingOrder()
    {
        // Arrange
        var child = await CreateTestChild(balance: 100.00m);

        await _transactionService.CreateTransactionAsync(new CreateTransactionDto(
            child.Id, 10.00m, TransactionType.Credit, TransactionCategory.Allowance, "First"));

        await Task.Delay(10); // Ensure different timestamps

        await _transactionService.CreateTransactionAsync(new CreateTransactionDto(
            child.Id, 5.00m, TransactionType.Debit, TransactionCategory.OtherSpending, "Second"));

        await Task.Delay(10);

        await _transactionService.CreateTransactionAsync(new CreateTransactionDto(
            child.Id, 20.00m, TransactionType.Credit, TransactionCategory.Allowance, "Third"));

        // Act
        var transactions = await _transactionService.GetChildTransactionsAsync(child.Id);

        // Assert
        transactions.Should().HaveCount(3);
        transactions[0].Description.Should().Be("Third");
        transactions[1].Description.Should().Be("Second");
        transactions[2].Description.Should().Be("First");
    }

    [Fact]
    public async Task GetChildTransactions_RespectsLimit()
    {
        // Arrange
        var child = await CreateTestChild(balance: 100.00m);

        for (int i = 0; i < 25; i++)
        {
            await _transactionService.CreateTransactionAsync(new CreateTransactionDto(
                child.Id, 1.00m, TransactionType.Credit, TransactionCategory.Allowance, $"Transaction {i}"));
        }

        // Act
        var transactions = await _transactionService.GetChildTransactionsAsync(child.Id, limit: 10);

        // Assert
        transactions.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetCurrentBalance_ReturnsCorrectBalance()
    {
        // Arrange
        var child = await CreateTestChild(balance: 50.00m);

        // Act
        var balance = await _transactionService.GetCurrentBalanceAsync(child.Id);

        // Assert
        balance.Should().Be(50.00m);
    }

    [Fact]
    public async Task CreateTransaction_UsesAtomicOperation()
    {
        // Arrange
        var child = await CreateTestChild(balance: 50.00m);
        var dto = new CreateTransactionDto(
            child.Id,
            25.00m,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            "Test credit");

        // Act
        var transaction = await _transactionService.CreateTransactionAsync(dto);

        // Assert - Transaction should be saved atomically
        var savedTransaction = await _context.Transactions.FindAsync(transaction.Id);
        savedTransaction.Should().NotBeNull();
        savedTransaction!.BalanceAfter.Should().Be(75.00m);

        var savedChild = await _context.Children.FindAsync(child.Id);
        savedChild!.CurrentBalance.Should().Be(75.00m);
    }

    private async Task<Child> CreateTestChild(decimal balance = 0m, decimal weeklyAllowance = 10m)
    {
        var family = new Family { Name = "Test Family" };
        _context.Families.Add(family);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        _context.Users.Add(user);

        var child = new Child
        {
            UserId = user.Id,
            FamilyId = family.Id,
            CurrentBalance = balance,
            WeeklyAllowance = weeklyAllowance
        };
        _context.Children.Add(child);

        await _context.SaveChangesAsync();
        return child;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
