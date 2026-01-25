using AllowanceTracker.Data;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Services;

/// <summary>
/// Integration tests for AllowanceService + SavingsAccountService
/// Tests automatic savings transfers when allowance is paid
/// </summary>
public class AllowanceSavingsIntegrationTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<ILogger<AllowanceService>> _mockLogger;
    private readonly ITransactionService _transactionService;
    private readonly ISavingsAccountService _savingsAccountService;
    private readonly IAllowanceService _allowanceService;
    private readonly Guid _currentUserId;

    public AllowanceSavingsIntegrationTests()
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

        // Setup mock logger
        _mockLogger = new Mock<ILogger<AllowanceService>>();

        // Create REAL services for integration testing
        _transactionService = new TransactionService(_context, _mockCurrentUser.Object);
        _savingsAccountService = new SavingsAccountService(_context);
        _allowanceService = new AllowanceService(
            _context,
            _mockCurrentUser.Object,
            _transactionService,
            _mockLogger.Object,
            _savingsAccountService);
    }

    [Fact]
    public async Task PayAllowance_WithFixedAmountSavings_TransfersCorrectAmount()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 0m);

        // Enable savings with fixed $5 transfer
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Act - Pay allowance (should trigger automatic transfer)
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(15m); // 20 - 5 transferred to savings
        updatedChild.SavingsBalance.Should().Be(5m);

        // Verify savings transaction was created
        var savingsTransactions = await _context.SavingsTransactions
            .Where(t => t.ChildId == child.Id)
            .ToListAsync();
        savingsTransactions.Should().HaveCount(1);
        savingsTransactions[0].Amount.Should().Be(5m);
        savingsTransactions[0].Type.Should().Be(SavingsTransactionType.AutoTransfer);
        savingsTransactions[0].IsAutomatic.Should().BeTrue();
    }

    [Fact]
    public async Task PayAllowance_WithPercentageSavings_CalculatesAndTransfersCorrectly()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 25m, balance: 0m);

        // Enable savings with 20% transfer
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.Percentage, 20m);

        // Act - Pay allowance
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(20m); // 25 - 5 (20% of 25)
        updatedChild.SavingsBalance.Should().Be(5m);

        // Verify savings transaction
        var savingsTransactions = await _context.SavingsTransactions
            .Where(t => t.ChildId == child.Id)
            .ToListAsync();
        savingsTransactions.Should().HaveCount(1);
        savingsTransactions[0].Amount.Should().Be(5m);
        savingsTransactions[0].Type.Should().Be(SavingsTransactionType.AutoTransfer);
    }

    [Fact]
    public async Task PayAllowance_WithSavingsDisabled_NoTransferOccurs()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 0m);
        // Savings account NOT enabled

        // Act - Pay allowance
        await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(20m); // Full amount, no transfer
        updatedChild.SavingsBalance.Should().Be(0m);

        // Verify NO savings transaction was created
        var savingsTransactions = await _context.SavingsTransactions
            .Where(t => t.ChildId == child.Id)
            .ToListAsync();
        savingsTransactions.Should().BeEmpty();
    }

    // Helper Methods
    private async Task<Child> CreateChild(
        decimal weeklyAllowance = 10m,
        decimal balance = 0m,
        decimal savingsBalance = 0m)
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family",
            CreatedAt = DateTime.UtcNow
        };
        _context.Families.Add(family);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testchild@test.com",
            Email = "testchild@test.com",
            FirstName = "Test",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        _context.Users.Add(user);

        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = family.Id,
            WeeklyAllowance = weeklyAllowance,
            CurrentBalance = balance,
            SavingsBalance = savingsBalance,
            CreatedAt = DateTime.UtcNow
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
