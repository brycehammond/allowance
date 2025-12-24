using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class SavingsAccountServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly ISavingsAccountService _savingsAccountService;
    private readonly Guid _parentUserId = Guid.NewGuid();

    public SavingsAccountServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);
        _savingsAccountService = new SavingsAccountService(_context);
    }

    // Configuration Tests (5)
    [Fact]
    public async Task EnableSavingsAccount_FixedAmount_ConfiguresCorrectly()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m);

        // Act
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsAccountEnabled.Should().BeTrue();
        updated.SavingsTransferType.Should().Be(SavingsTransferType.FixedAmount);
        updated.SavingsTransferAmount.Should().Be(5m);
        updated.SavingsTransferPercentage.Should().Be(0);
    }

    [Fact]
    public async Task EnableSavingsAccount_Percentage_ConfiguresCorrectly()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m);

        // Act
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.Percentage, 20m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsAccountEnabled.Should().BeTrue();
        updated.SavingsTransferType.Should().Be(SavingsTransferType.Percentage);
        updated.SavingsTransferAmount.Should().Be(0);
        updated.SavingsTransferPercentage.Should().Be(20);
    }

    [Fact]
    public async Task DisableSavingsAccount_DisablesButKeepsBalance()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m, savingsBalance: 50m);
        child.SavingsAccountEnabled = true;
        child.SavingsTransferType = SavingsTransferType.FixedAmount;
        child.SavingsTransferAmount = 5m;
        await _context.SaveChangesAsync();

        // Act
        await _savingsAccountService.DisableSavingsAccountAsync(child.Id);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsAccountEnabled.Should().BeFalse();
        updated.SavingsBalance.Should().Be(50m); // Balance preserved
        updated.SavingsTransferType.Should().Be(SavingsTransferType.None);
    }

    [Fact]
    public async Task UpdateSavingsConfig_UpdatesSettings()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Act
        await _savingsAccountService.UpdateSavingsConfigAsync(
            child.Id, SavingsTransferType.Percentage, 25m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsTransferType.Should().Be(SavingsTransferType.Percentage);
        updated.SavingsTransferPercentage.Should().Be(25);
        updated.SavingsTransferAmount.Should().Be(0);
    }

    [Fact]
    public async Task EnableSavingsAccount_InvalidPercentage_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m);

        // Act
        var act = () => _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.Percentage, 150m);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // Manual Transaction Tests (4)
    [Fact]
    public async Task DepositToSavings_ValidAmount_IncreasesBalance()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m, savingsBalance: 0m);
        child.SavingsAccountEnabled = true;
        await _context.SaveChangesAsync();

        // Act
        var transaction = await _savingsAccountService.DepositToSavingsAsync(
            child.Id, 25m, "Manual deposit", _parentUserId);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(25m);
        transaction.BalanceAfter.Should().Be(25m);
        transaction.Type.Should().Be(SavingsTransactionType.Deposit);
        transaction.IsAutomatic.Should().BeFalse();

        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(25m);
        updated.CurrentBalance.Should().Be(75m); // 100 - 25
    }

    [Fact]
    public async Task DepositToSavings_InsufficientBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 10m, savingsBalance: 0m);
        child.SavingsAccountEnabled = true;
        await _context.SaveChangesAsync();

        // Act
        var act = () => _savingsAccountService.DepositToSavingsAsync(
            child.Id, 50m, "Too much", _parentUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient*");
    }

    [Fact]
    public async Task WithdrawFromSavings_ValidAmount_DecreasesBalance()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 50m, savingsBalance: 40m);
        child.SavingsAccountEnabled = true;
        await _context.SaveChangesAsync();

        // Act
        var transaction = await _savingsAccountService.WithdrawFromSavingsAsync(
            child.Id, 15m, "Need money", _parentUserId);

        // Assert
        transaction.Amount.Should().Be(15m);
        transaction.BalanceAfter.Should().Be(25m); // 40 - 15
        transaction.Type.Should().Be(SavingsTransactionType.Withdrawal);

        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(25m);
        updated.CurrentBalance.Should().Be(65m); // 50 + 15
    }

    [Fact]
    public async Task WithdrawFromSavings_ExceedsSavingsBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 50m, savingsBalance: 10m);
        child.SavingsAccountEnabled = true;
        await _context.SaveChangesAsync();

        // Act
        var act = () => _savingsAccountService.WithdrawFromSavingsAsync(
            child.Id, 20m, "Too much", _parentUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient*");
    }

    // Automatic Transfer Tests (4)
    [Fact]
    public async Task ProcessAutomaticTransfer_FixedAmount_TransfersCorrectAmount()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 120m, savingsBalance: 0m);
        child.SavingsAccountEnabled = true;
        child.SavingsTransferType = SavingsTransferType.FixedAmount;
        child.SavingsTransferAmount = 5m;
        await _context.SaveChangesAsync();

        var allowanceTransactionId = Guid.NewGuid();

        // Act
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            child.Id, allowanceTransactionId, 20m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(5m);
        updated.CurrentBalance.Should().Be(115m); // 120 - 5

        var transactions = await _context.SavingsTransactions
            .Where(t => t.ChildId == child.Id)
            .ToListAsync();
        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().Be(5m);
        transactions[0].Type.Should().Be(SavingsTransactionType.AutoTransfer);
        transactions[0].IsAutomatic.Should().BeTrue();
        transactions[0].SourceAllowanceTransactionId.Should().Be(allowanceTransactionId);
    }

    [Fact]
    public async Task ProcessAutomaticTransfer_Percentage_CalculatesCorrectly()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 120m, savingsBalance: 0m);
        child.SavingsAccountEnabled = true;
        child.SavingsTransferType = SavingsTransferType.Percentage;
        child.SavingsTransferPercentage = 25;
        await _context.SaveChangesAsync();

        var allowanceTransactionId = Guid.NewGuid();

        // Act
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            child.Id, allowanceTransactionId, 20m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(5m); // 25% of 20
        updated.CurrentBalance.Should().Be(115m); // 120 - 5
    }

    [Fact]
    public async Task ProcessAutomaticTransfer_NotEnabled_DoesNothing()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 120m, savingsBalance: 0m);
        // Not enabled
        var allowanceTransactionId = Guid.NewGuid();

        // Act
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            child.Id, allowanceTransactionId, 20m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(0m); // No transfer
        updated.CurrentBalance.Should().Be(120m); // Unchanged
    }

    [Fact]
    public async Task ProcessAutomaticTransfer_InsufficientBalance_SkipsTransfer()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 3m, savingsBalance: 0m);
        child.SavingsAccountEnabled = true;
        child.SavingsTransferType = SavingsTransferType.FixedAmount;
        child.SavingsTransferAmount = 10m;
        await _context.SaveChangesAsync();

        var allowanceTransactionId = Guid.NewGuid();

        // Act
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            child.Id, allowanceTransactionId, 20m);

        // Assert - Transfer skipped due to insufficient balance
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(0m);
        updated.CurrentBalance.Should().Be(3m);
    }

    // Calculation & Validation Tests (2)
    [Fact]
    public void CalculateTransferAmount_FixedAmount_ReturnsFixedValue()
    {
        // Act
        var amount = _savingsAccountService.CalculateTransferAmount(
            allowanceAmount: 20m,
            type: SavingsTransferType.FixedAmount,
            configValue: 5m);

        // Assert
        amount.Should().Be(5m);
    }

    [Fact]
    public void CalculateTransferAmount_Percentage_CalculatesCorrectly()
    {
        // Act
        var amount = _savingsAccountService.CalculateTransferAmount(
            allowanceAmount: 25m,
            type: SavingsTransferType.Percentage,
            configValue: 20m);

        // Assert
        amount.Should().Be(5m); // 20% of 25
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
