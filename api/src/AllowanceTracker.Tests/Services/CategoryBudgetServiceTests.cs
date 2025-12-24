using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Tests.Services;

public class CategoryBudgetServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly ICategoryBudgetService _budgetService;
    private readonly Guid _parentUserId;
    private readonly Guid _otherParentUserId;

    public CategoryBudgetServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AllowanceContext(options);
        _budgetService = new CategoryBudgetService(_context);
        _parentUserId = Guid.NewGuid();
        _otherParentUserId = Guid.NewGuid();
    }

    #region SetBudgetAsync Tests

    [Fact]
    public async Task SetBudget_CreatesNewBudget_WhenNoneExists()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);
        var dto = new SetBudgetDto(
            child.Id,
            TransactionCategory.Toys,
            50m,
            BudgetPeriod.Weekly,
            80,
            false);

        // Act
        var result = await _budgetService.SetBudgetAsync(dto, _parentUserId);

        // Assert
        result.Should().NotBeNull();
        result.ChildId.Should().Be(child.Id);
        result.Category.Should().Be(TransactionCategory.Toys);
        result.Limit.Should().Be(50m);
        result.Period.Should().Be(BudgetPeriod.Weekly);
        result.AlertThresholdPercent.Should().Be(80);
        result.EnforceLimit.Should().BeFalse();
        result.CreatedById.Should().Be(_parentUserId);
    }

    [Fact]
    public async Task SetBudget_UpdatesExistingBudget_WhenAlreadyExists()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);
        var existingBudget = new CategoryBudget
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Category = TransactionCategory.Candy,
            Limit = 10m,
            Period = BudgetPeriod.Weekly,
            AlertThresholdPercent = 80,
            EnforceLimit = false,
            CreatedById = _parentUserId
        };
        _context.CategoryBudgets.Add(existingBudget);
        await _context.SaveChangesAsync();

        var dto = new SetBudgetDto(
            child.Id,
            TransactionCategory.Candy,
            15m,
            BudgetPeriod.Weekly,
            90,
            true);

        // Act
        var result = await _budgetService.SetBudgetAsync(dto, _parentUserId);

        // Assert
        result.Id.Should().Be(existingBudget.Id); // Same ID = update
        result.Limit.Should().Be(15m);
        result.AlertThresholdPercent.Should().Be(90);
        result.EnforceLimit.Should().BeTrue();
    }

    [Fact]
    public async Task SetBudget_ThrowsException_WhenUserUnauthorized()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);
        var dto = new SetBudgetDto(
            child.Id,
            TransactionCategory.Toys,
            50m,
            BudgetPeriod.Weekly);

        // Act
        var act = () => _budgetService.SetBudgetAsync(dto, _otherParentUserId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SetBudget_ThrowsException_WhenChildNotFound()
    {
        // Arrange
        var dto = new SetBudgetDto(
            Guid.NewGuid(),
            TransactionCategory.Toys,
            50m,
            BudgetPeriod.Weekly);

        // Act
        var act = () => _budgetService.SetBudgetAsync(dto, _parentUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Child not found");
    }

    #endregion

    #region GetBudgetAsync Tests

    [Fact]
    public async Task GetBudget_ReturnsBudget_WhenExists()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);
        var budget = new CategoryBudget
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Category = TransactionCategory.Games,
            Limit = 30m,
            Period = BudgetPeriod.Monthly,
            AlertThresholdPercent = 75,
            EnforceLimit = true,
            CreatedById = _parentUserId
        };
        _context.CategoryBudgets.Add(budget);
        await _context.SaveChangesAsync();

        // Act
        var result = await _budgetService.GetBudgetAsync(child.Id, TransactionCategory.Games);

        // Assert
        result.Should().NotBeNull();
        result!.Limit.Should().Be(30m);
        result.Period.Should().Be(BudgetPeriod.Monthly);
        result.EnforceLimit.Should().BeTrue();
    }

    [Fact]
    public async Task GetBudget_ReturnsNull_WhenDoesNotExist()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);

        // Act
        var result = await _budgetService.GetBudgetAsync(child.Id, TransactionCategory.Books);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllBudgetsAsync Tests

    [Fact]
    public async Task GetAllBudgets_ReturnsAllBudgetsForChild()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);
        var budgets = new[]
        {
            new CategoryBudget
            {
                Id = Guid.NewGuid(),
                ChildId = child.Id,
                Category = TransactionCategory.Toys,
                Limit = 50m,
                Period = BudgetPeriod.Weekly,
                CreatedById = _parentUserId
            },
            new CategoryBudget
            {
                Id = Guid.NewGuid(),
                ChildId = child.Id,
                Category = TransactionCategory.Candy,
                Limit = 10m,
                Period = BudgetPeriod.Weekly,
                CreatedById = _parentUserId
            }
        };
        _context.CategoryBudgets.AddRange(budgets);
        await _context.SaveChangesAsync();

        // Act
        var result = await _budgetService.GetAllBudgetsAsync(child.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Category == TransactionCategory.Toys);
        result.Should().Contain(b => b.Category == TransactionCategory.Candy);
    }

    [Fact]
    public async Task GetAllBudgets_ReturnsEmpty_WhenNoBudgets()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);

        // Act
        var result = await _budgetService.GetAllBudgetsAsync(child.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region DeleteBudgetAsync Tests

    [Fact]
    public async Task DeleteBudget_DeletesSuccessfully_WhenAuthorized()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);
        var budget = new CategoryBudget
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Category = TransactionCategory.Snacks,
            Limit = 20m,
            Period = BudgetPeriod.Weekly,
            CreatedById = _parentUserId
        };
        _context.CategoryBudgets.Add(budget);
        await _context.SaveChangesAsync();

        // Act
        await _budgetService.DeleteBudgetAsync(budget.Id, _parentUserId);

        // Assert
        var deleted = await _context.CategoryBudgets.FindAsync(budget.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBudget_ThrowsException_WhenUnauthorized()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);
        var budget = new CategoryBudget
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Category = TransactionCategory.Snacks,
            Limit = 20m,
            Period = BudgetPeriod.Weekly,
            CreatedById = _parentUserId
        };
        _context.CategoryBudgets.Add(budget);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _budgetService.DeleteBudgetAsync(budget.Id, _otherParentUserId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteBudget_ThrowsException_WhenBudgetNotFound()
    {
        // Act
        var act = () => _budgetService.DeleteBudgetAsync(Guid.NewGuid(), _parentUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Budget not found");
    }

    #endregion

    #region CanManageBudgetsAsync Tests

    [Fact]
    public async Task CanManageBudgets_ReturnsTrue_ForParentInSameFamily()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);

        // Act
        var result = await _budgetService.CanManageBudgetsAsync(child.Id, _parentUserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageBudgets_ReturnsFalse_ForParentInDifferentFamily()
    {
        // Arrange
        var child = await CreateChildAsync(_parentUserId);

        // Act
        var result = await _budgetService.CanManageBudgetsAsync(child.Id, _otherParentUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageBudgets_ReturnsFalse_WhenChildNotFound()
    {
        // Act
        var result = await _budgetService.CanManageBudgetsAsync(Guid.NewGuid(), _parentUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private async Task<Child> CreateChildAsync(Guid parentUserId)
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family"
        };
        _context.Families.Add(family);

        var parentUser = new ApplicationUser
        {
            Id = parentUserId,
            UserName = $"parent{parentUserId}@test.com",
            Email = $"parent{parentUserId}@test.com",
            FirstName = "Parent",
            LastName = "User",
            Role = UserRole.Parent,
            FamilyId = family.Id
        };
        _context.Users.Add(parentUser);

        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "child@test.com",
            Email = "child@test.com",
            FirstName = "Test",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        _context.Users.Add(childUser);

        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 100m
        };
        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        return child;
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
