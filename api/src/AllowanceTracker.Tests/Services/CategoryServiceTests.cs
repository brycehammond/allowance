using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Tests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly ICategoryService _categoryService;
    private readonly Guid _testUserId;

    public CategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AllowanceContext(options);
        _categoryService = new CategoryService(_context);
        _testUserId = Guid.NewGuid();
    }

    #region GetCategorySpending Tests

    [Fact]
    public async Task GetCategorySpending_WithMultipleCategories_ReturnsCorrectBreakdown()
    {
        // Arrange
        var child = await CreateChildAsync();
        await CreateTransactionAsync(child.Id, 10m, TransactionType.Debit, TransactionCategory.Toys, "Toy car");
        await CreateTransactionAsync(child.Id, 20m, TransactionType.Debit, TransactionCategory.Games, "Video game");
        await CreateTransactionAsync(child.Id, 10m, TransactionType.Debit, TransactionCategory.Toys, "Lego set");

        // Act
        var result = await _categoryService.GetCategorySpendingAsync(child.Id);

        // Assert
        result.Should().HaveCount(2);

        // Games should be first (highest amount)
        result[0].Category.Should().Be(TransactionCategory.Games);
        result[0].TotalAmount.Should().Be(20m);
        result[0].TransactionCount.Should().Be(1);
        result[0].Percentage.Should().Be(50m); // 20/40 * 100

        // Toys should be second
        result[1].Category.Should().Be(TransactionCategory.Toys);
        result[1].TotalAmount.Should().Be(20m); // 10 + 10
        result[1].TransactionCount.Should().Be(2);
        result[1].Percentage.Should().Be(50m); // 20/40 * 100
    }

    [Fact]
    public async Task GetCategorySpending_WithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var child = await CreateChildAsync();

        // Act
        var result = await _categoryService.GetCategorySpendingAsync(child.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategorySpending_OnlyIncludesDebits_IgnoresCredits()
    {
        // Arrange
        var child = await CreateChildAsync();
        await CreateTransactionAsync(child.Id, 50m, TransactionType.Credit, TransactionCategory.Allowance, "Weekly allowance");
        await CreateTransactionAsync(child.Id, 10m, TransactionType.Debit, TransactionCategory.Toys, "Toy");

        // Act
        var result = await _categoryService.GetCategorySpendingAsync(child.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be(TransactionCategory.Toys);
        result[0].TotalAmount.Should().Be(10m);
    }

    #endregion

    #region GetBudgetStatus Tests

    [Fact]
    public async Task GetBudgetStatus_WithSpendingUnderLimit_ReturnsCorrectStatus()
    {
        // Arrange
        var child = await CreateChildAsync();
        await CreateBudgetAsync(child.Id, TransactionCategory.Toys, 100m, BudgetPeriod.Weekly);

        // Create transaction within last 7 days (weekly period)
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Amount = 50m,
            Type = TransactionType.Debit,
            Category = TransactionCategory.Toys,
            Description = "Toy",
            BalanceAfter = 50m,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-2) // Within weekly period
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetBudgetStatusAsync(child.Id, BudgetPeriod.Weekly);

        // Assert
        var status = result.Should().ContainSingle().Subject;
        status.Category.Should().Be(TransactionCategory.Toys);
        status.BudgetLimit.Should().Be(100m);
        status.CurrentSpending.Should().Be(50m);
        status.Remaining.Should().Be(50m);
        status.PercentUsed.Should().Be(50);
        status.Status.Should().Be(BudgetStatus.Safe);
    }

    [Fact]
    public async Task GetBudgetStatus_WithSpendingOverLimit_ReturnsOverBudgetStatus()
    {
        // Arrange
        var child = await CreateChildAsync();
        await CreateBudgetAsync(child.Id, TransactionCategory.Snacks, 20m, BudgetPeriod.Weekly);

        // Create transaction that exceeds budget
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Amount = 25m,
            Type = TransactionType.Debit,
            Category = TransactionCategory.Snacks,
            Description = "Snacks",
            BalanceAfter = 75m,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetBudgetStatusAsync(child.Id, BudgetPeriod.Weekly);

        // Assert
        var status = result.Should().ContainSingle().Subject;
        status.Status.Should().Be(BudgetStatus.OverBudget);
        status.PercentUsed.Should().Be(125);
        status.Remaining.Should().Be(-5m);
    }

    #endregion

    #region CheckBudget Tests

    [Fact]
    public async Task CheckBudget_WithEnforcedLimit_PreventsBudgetExceeding()
    {
        // Arrange
        var child = await CreateChildAsync();
        await CreateBudgetAsync(child.Id, TransactionCategory.Candy, 10m, BudgetPeriod.Weekly, enforceLimit: true);

        // Already spent $8
        var existingTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Amount = 8m,
            Type = TransactionType.Debit,
            Category = TransactionCategory.Candy,
            Description = "Candy",
            BalanceAfter = 92m,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Transactions.Add(existingTransaction);
        await _context.SaveChangesAsync();

        // Act - Try to spend $5 more (would exceed $10 limit)
        var result = await _categoryService.CheckBudgetAsync(child.Id, TransactionCategory.Candy, 5m);

        // Assert
        result.Allowed.Should().BeFalse();
        result.Message.Should().Contain("exceeds budget");
        result.CurrentSpending.Should().Be(8m);
        result.BudgetLimit.Should().Be(10m);
        result.RemainingAfter.Should().Be(-3m);
    }

    [Fact]
    public async Task CheckBudget_WithoutEnforcedLimit_AllowsTransaction()
    {
        // Arrange
        var child = await CreateChildAsync();
        await CreateBudgetAsync(child.Id, TransactionCategory.Toys, 50m, BudgetPeriod.Weekly, enforceLimit: false);

        var existingTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Amount = 45m,
            Type = TransactionType.Debit,
            Category = TransactionCategory.Toys,
            Description = "Toy",
            BalanceAfter = 55m,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Transactions.Add(existingTransaction);
        await _context.SaveChangesAsync();

        // Act - Try to spend $10 more (would exceed limit but not enforced)
        var result = await _categoryService.CheckBudgetAsync(child.Id, TransactionCategory.Toys, 10m);

        // Assert
        result.Allowed.Should().BeTrue();
        result.Message.Should().Contain("No budget limit");
    }

    [Fact]
    public async Task CheckBudget_WithNoBudgetSet_AllowsTransaction()
    {
        // Arrange
        var child = await CreateChildAsync();

        // Act - No budget set for Games category
        var result = await _categoryService.CheckBudgetAsync(child.Id, TransactionCategory.Games, 100m);

        // Assert
        result.Allowed.Should().BeTrue();
        result.Message.Should().Contain("No budget limit");
    }

    #endregion

    #region SuggestCategory Tests

    [Fact]
    public void SuggestCategory_WithToyDescription_ReturnsToys()
    {
        // Act
        var result = _categoryService.SuggestCategory("Bought a toy car", TransactionType.Debit);

        // Assert
        result.Should().Be(TransactionCategory.Toys);
    }

    [Fact]
    public void SuggestCategory_WithAllowanceDescription_ReturnsAllowance()
    {
        // Act
        var result = _categoryService.SuggestCategory("Weekly allowance", TransactionType.Credit);

        // Assert
        result.Should().Be(TransactionCategory.Allowance);
    }

    #endregion

    #region GetCategoriesForType Tests

    [Fact]
    public void GetCategoriesForType_Credit_ReturnsIncomeCategories()
    {
        // Act
        var result = _categoryService.GetCategoriesForType(TransactionType.Credit);

        // Assert
        result.Should().Contain(TransactionCategory.Allowance);
        result.Should().Contain(TransactionCategory.Chores);
        result.Should().Contain(TransactionCategory.Gift);
        result.Should().NotContain(TransactionCategory.Toys);
        result.Should().NotContain(TransactionCategory.Games);
    }

    [Fact]
    public void GetCategoriesForType_Debit_ReturnsSpendingCategories()
    {
        // Act
        var result = _categoryService.GetCategoriesForType(TransactionType.Debit);

        // Assert
        result.Should().Contain(TransactionCategory.Toys);
        result.Should().Contain(TransactionCategory.Games);
        result.Should().Contain(TransactionCategory.Snacks);
        result.Should().NotContain(TransactionCategory.Allowance);
        result.Should().NotContain(TransactionCategory.Chores);
    }

    #endregion

    #region Helper Methods

    private async Task<Child> CreateChildAsync()
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family"
        };
        _context.Families.Add(family);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testchild@test.com",
            Email = "testchild@test.com",
            FirstName = "Test",
            LastName = "Child",
            FamilyId = family.Id
        };
        _context.Users.Add(user);

        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 100m
        };
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        return child;
    }

    private async Task<Transaction> CreateTransactionAsync(
        Guid childId,
        decimal amount,
        TransactionType type,
        TransactionCategory category,
        string description)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Amount = amount,
            Type = type,
            Category = category,
            Description = description,
            BalanceAfter = 100m,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    private async Task<CategoryBudget> CreateBudgetAsync(
        Guid childId,
        TransactionCategory category,
        decimal limit,
        BudgetPeriod period,
        bool enforceLimit = false)
    {
        var budget = new CategoryBudget
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Category = category,
            Limit = limit,
            Period = period,
            AlertThresholdPercent = 80,
            EnforceLimit = enforceLimit,
            CreatedById = _testUserId
        };

        _context.CategoryBudgets.Add(budget);
        await _context.SaveChangesAsync();
        return budget;
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
