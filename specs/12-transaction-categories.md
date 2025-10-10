# Transaction Categories & Budgeting Specification

## Overview

This specification introduces transaction categorization and budget management features to the Allowance Tracker application. Categories help children understand where their money goes, while budgets teach them to plan and limit spending in specific areas.

## Goals

1. **Financial Awareness**: Help children see spending patterns
2. **Budget Planning**: Teach children to allocate money across categories
3. **Parent Control**: Allow parents to set spending limits
4. **Analytics**: Enable category-based insights and charts
5. **TDD Approach**: Full test coverage with 25+ tests

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Entity Framework Core
- **Database**: PostgreSQL with enum support
- **Testing**: xUnit, FluentAssertions, Moq
- **Charts**: Blazor-ApexCharts for pie/donut charts (already added)
- **UI**: Blazor Server components

---

## Phase 1: Database Schema & Models

### 1.1 TransactionCategory Enum

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Categories for transaction classification
/// </summary>
public enum TransactionCategory
{
    // Income categories
    Allowance = 1,
    Chores = 2,
    Gift = 3,
    BonusReward = 4,
    OtherIncome = 5,

    // Spending categories
    Toys = 10,
    Games = 11,
    Books = 12,
    Clothes = 13,
    Snacks = 14,
    Candy = 15,
    Electronics = 16,
    Entertainment = 17,
    Sports = 18,
    Crafts = 19,
    OtherSpending = 20,

    // Savings & Giving categories
    Savings = 30,
    Charity = 31,
    Investment = 32
}
```

### 1.2 Update Transaction Model

```csharp
namespace AllowanceTracker.Models;

public class Transaction
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; } // Credit or Debit

    // NEW: Category field
    public TransactionCategory Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}
```

### 1.3 CategoryBudget Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Budget limits for specific categories per child
/// </summary>
public class CategoryBudget
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public TransactionCategory Category { get; set; }

    /// <summary>
    /// Budget limit per period (e.g., $20/week for Snacks)
    /// </summary>
    public decimal Limit { get; set; }

    /// <summary>
    /// Budget period (Weekly, Monthly)
    /// </summary>
    public BudgetPeriod Period { get; set; }

    /// <summary>
    /// Alert when spending reaches X% of limit (e.g., 80%)
    /// </summary>
    public int AlertThresholdPercent { get; set; } = 80;

    /// <summary>
    /// Enforce hard limit (prevent transactions over budget)
    /// </summary>
    public bool EnforceLimit { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}

public enum BudgetPeriod
{
    Weekly = 1,
    Monthly = 2
}
```

### 1.4 Update Child Model

```csharp
namespace AllowanceTracker.Models;

public class Child
{
    // ... existing properties ...

    // NEW: Navigation property for budgets
    public virtual ICollection<CategoryBudget> CategoryBudgets { get; set; } = new List<CategoryBudget>();
}
```

### 1.5 Database Migration

```bash
# Generate migration
dotnet ef migrations add AddTransactionCategories

# Expected changes:
# - Add Category column to Transactions table (enum stored as int)
# - Create CategoryBudgets table
# - Add foreign keys and indexes
```

```csharp
// Migration example
public partial class AddTransactionCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add Category to Transaction
        migrationBuilder.AddColumn<int>(
            name: "Category",
            table: "Transactions",
            type: "integer",
            nullable: false,
            defaultValue: 20); // OtherSpending

        // Create CategoryBudgets table
        migrationBuilder.CreateTable(
            name: "CategoryBudgets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                Limit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Period = table.Column<int>(type: "integer", nullable: false),
                AlertThresholdPercent = table.Column<int>(type: "integer", nullable: false),
                EnforceLimit = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CategoryBudgets", x => x.Id);
                table.ForeignKey(
                    name: "FK_CategoryBudgets_Children_ChildId",
                    column: x => x.ChildId,
                    principalTable: "Children",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CategoryBudgets_AspNetUsers_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Add indexes
        migrationBuilder.CreateIndex(
            name: "IX_CategoryBudgets_ChildId_Category",
            table: "CategoryBudgets",
            columns: new[] { "ChildId", "Category" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_Category",
            table: "Transactions",
            column: "Category");
    }
}
```

---

## Phase 2: Service Layer (TDD)

### 2.1 ICategoryService Interface

```csharp
namespace AllowanceTracker.Services;

public interface ICategoryService
{
    /// <summary>
    /// Get spending breakdown by category for a date range
    /// </summary>
    Task<List<CategorySpendingDto>> GetCategorySpendingAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Get current spending vs budget for all categories
    /// </summary>
    Task<List<CategoryBudgetStatusDto>> GetBudgetStatusAsync(
        Guid childId,
        BudgetPeriod period);

    /// <summary>
    /// Check if a transaction would exceed budget
    /// </summary>
    Task<BudgetCheckResult> CheckBudgetAsync(
        Guid childId,
        TransactionCategory category,
        decimal amount);

    /// <summary>
    /// Get suggested category for a transaction based on description
    /// </summary>
    TransactionCategory SuggestCategory(string description, TransactionType type);

    /// <summary>
    /// Get all categories appropriate for transaction type
    /// </summary>
    List<TransactionCategory> GetCategoriesForType(TransactionType type);
}
```

### 2.2 ICategoryBudgetService Interface

```csharp
namespace AllowanceTracker.Services;

public interface ICategoryBudgetService
{
    /// <summary>
    /// Create or update a budget for a category
    /// </summary>
    Task<CategoryBudget> SetBudgetAsync(SetBudgetDto dto, Guid currentUserId);

    /// <summary>
    /// Get budget for a specific category
    /// </summary>
    Task<CategoryBudget?> GetBudgetAsync(Guid childId, TransactionCategory category);

    /// <summary>
    /// Get all budgets for a child
    /// </summary>
    Task<List<CategoryBudget>> GetAllBudgetsAsync(Guid childId);

    /// <summary>
    /// Delete a budget
    /// </summary>
    Task DeleteBudgetAsync(Guid budgetId, Guid currentUserId);

    /// <summary>
    /// Check if parent can manage budgets for child
    /// </summary>
    Task<bool> CanManageBudgetsAsync(Guid childId, Guid userId);
}
```

### 2.3 Data Transfer Objects

```csharp
namespace AllowanceTracker.DTOs;

// Category spending summary
public record CategorySpendingDto(
    TransactionCategory Category,
    string CategoryName,
    decimal TotalAmount,
    int TransactionCount,
    decimal Percentage);

// Budget status with current spending
public record CategoryBudgetStatusDto(
    TransactionCategory Category,
    string CategoryName,
    decimal BudgetLimit,
    decimal CurrentSpending,
    decimal Remaining,
    int PercentUsed,
    BudgetStatus Status,
    BudgetPeriod Period);

public enum BudgetStatus
{
    Safe,        // < 80% used
    Warning,     // 80-99% used
    AtLimit,     // 100% used
    OverBudget   // > 100% used
}

// Budget check result
public record BudgetCheckResult(
    bool Allowed,
    string Message,
    decimal CurrentSpending,
    decimal BudgetLimit,
    decimal RemainingAfter);

// Set budget DTO
public record SetBudgetDto(
    Guid ChildId,
    TransactionCategory Category,
    decimal Limit,
    BudgetPeriod Period,
    int AlertThresholdPercent = 80,
    bool EnforceLimit = false);

// Update transaction DTO (enhanced)
public record CreateTransactionDto(
    Guid ChildId,
    decimal Amount,
    TransactionType Type,
    TransactionCategory Category,
    string Description);
```

### 2.4 CategoryService Implementation (TDD)

```csharp
namespace AllowanceTracker.Services;

public class CategoryService : ICategoryService
{
    private readonly AllowanceContext _context;

    public CategoryService(AllowanceContext context)
    {
        _context = context;
    }

    public async Task<List<CategorySpendingDto>> GetCategorySpendingAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddMonths(-1);
        endDate ??= DateTime.UtcNow;

        var transactions = await _context.Transactions
            .Where(t => t.ChildId == childId
                && t.Type == TransactionType.Debit
                && t.CreatedAt >= startDate
                && t.CreatedAt <= endDate)
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .ToListAsync();

        var total = transactions.Sum(t => t.TotalAmount);

        return transactions
            .Select(t => new CategorySpendingDto(
                t.Category,
                GetCategoryName(t.Category),
                t.TotalAmount,
                t.TransactionCount,
                total > 0 ? (t.TotalAmount / total) * 100 : 0))
            .OrderByDescending(c => c.TotalAmount)
            .ToList();
    }

    public async Task<List<CategoryBudgetStatusDto>> GetBudgetStatusAsync(
        Guid childId,
        BudgetPeriod period)
    {
        var budgets = await _context.CategoryBudgets
            .Where(b => b.ChildId == childId && b.Period == period)
            .ToListAsync();

        var (startDate, endDate) = GetPeriodDates(period);

        var spending = await _context.Transactions
            .Where(t => t.ChildId == childId
                && t.Type == TransactionType.Debit
                && t.CreatedAt >= startDate
                && t.CreatedAt <= endDate)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.Category, x => x.Total);

        return budgets.Select(b =>
        {
            var current = spending.GetValueOrDefault(b.Category, 0);
            var remaining = b.Limit - current;
            var percentUsed = b.Limit > 0 ? (int)((current / b.Limit) * 100) : 0;

            var status = percentUsed switch
            {
                >= 100 => BudgetStatus.OverBudget,
                >= b.AlertThresholdPercent => BudgetStatus.Warning,
                _ => BudgetStatus.Safe
            };

            return new CategoryBudgetStatusDto(
                b.Category,
                GetCategoryName(b.Category),
                b.Limit,
                current,
                remaining,
                percentUsed,
                status,
                b.Period);
        })
        .OrderBy(s => s.Status)
        .ThenByDescending(s => s.PercentUsed)
        .ToList();
    }

    public async Task<BudgetCheckResult> CheckBudgetAsync(
        Guid childId,
        TransactionCategory category,
        decimal amount)
    {
        var budget = await _context.CategoryBudgets
            .FirstOrDefaultAsync(b => b.ChildId == childId && b.Category == category);

        if (budget == null || !budget.EnforceLimit)
        {
            return new BudgetCheckResult(
                Allowed: true,
                Message: "No budget limit set",
                CurrentSpending: 0,
                BudgetLimit: 0,
                RemainingAfter: 0);
        }

        var (startDate, _) = GetPeriodDates(budget.Period);

        var currentSpending = await _context.Transactions
            .Where(t => t.ChildId == childId
                && t.Category == category
                && t.Type == TransactionType.Debit
                && t.CreatedAt >= startDate)
            .SumAsync(t => t.Amount);

        var remainingAfter = budget.Limit - currentSpending - amount;
        var allowed = remainingAfter >= 0;

        var message = allowed
            ? remainingAfter < budget.Limit * 0.2m
                ? $"Transaction allowed. ${remainingAfter:F2} remaining in budget."
                : "Transaction allowed."
            : $"Transaction exceeds budget by ${Math.Abs(remainingAfter):F2}.";

        return new BudgetCheckResult(
            allowed,
            message,
            currentSpending,
            budget.Limit,
            remainingAfter);
    }

    public TransactionCategory SuggestCategory(string description, TransactionType type)
    {
        var lower = description.ToLowerInvariant();

        if (type == TransactionType.Credit)
        {
            if (lower.Contains("allowance")) return TransactionCategory.Allowance;
            if (lower.Contains("chore")) return TransactionCategory.Chores;
            if (lower.Contains("gift") || lower.Contains("birthday")) return TransactionCategory.Gift;
            return TransactionCategory.OtherIncome;
        }

        // Debit suggestions
        if (lower.Contains("toy") || lower.Contains("lego")) return TransactionCategory.Toys;
        if (lower.Contains("game") || lower.Contains("xbox") || lower.Contains("nintendo"))
            return TransactionCategory.Games;
        if (lower.Contains("book") || lower.Contains("reading")) return TransactionCategory.Books;
        if (lower.Contains("shirt") || lower.Contains("pants") || lower.Contains("clothes"))
            return TransactionCategory.Clothes;
        if (lower.Contains("snack") || lower.Contains("chips")) return TransactionCategory.Snacks;
        if (lower.Contains("candy") || lower.Contains("chocolate")) return TransactionCategory.Candy;
        if (lower.Contains("charity") || lower.Contains("donation")) return TransactionCategory.Charity;
        if (lower.Contains("saving")) return TransactionCategory.Savings;

        return TransactionCategory.OtherSpending;
    }

    public List<TransactionCategory> GetCategoriesForType(TransactionType type)
    {
        if (type == TransactionType.Credit)
        {
            return new List<TransactionCategory>
            {
                TransactionCategory.Allowance,
                TransactionCategory.Chores,
                TransactionCategory.Gift,
                TransactionCategory.BonusReward,
                TransactionCategory.OtherIncome
            };
        }

        return new List<TransactionCategory>
        {
            TransactionCategory.Toys,
            TransactionCategory.Games,
            TransactionCategory.Books,
            TransactionCategory.Clothes,
            TransactionCategory.Snacks,
            TransactionCategory.Candy,
            TransactionCategory.Electronics,
            TransactionCategory.Entertainment,
            TransactionCategory.Sports,
            TransactionCategory.Crafts,
            TransactionCategory.Savings,
            TransactionCategory.Charity,
            TransactionCategory.OtherSpending
        };
    }

    private (DateTime startDate, DateTime endDate) GetPeriodDates(BudgetPeriod period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            BudgetPeriod.Weekly => (now.AddDays(-7), now),
            BudgetPeriod.Monthly => (now.AddMonths(-1), now),
            _ => throw new ArgumentException("Invalid budget period")
        };
    }

    private string GetCategoryName(TransactionCategory category)
    {
        return category.ToString();
    }
}
```

### 2.5 Test Cases (12 Service Tests)

```csharp
namespace AllowanceTracker.Tests.Services;

public class CategoryServiceTests
{
    // GetCategorySpending Tests
    [Fact]
    public async Task GetCategorySpending_WithMultipleCategories_ReturnsCorrectBreakdown()
    {
        // Arrange
        var child = await CreateChild();
        await CreateTransaction(child.Id, 10m, TransactionCategory.Toys);
        await CreateTransaction(child.Id, 20m, TransactionCategory.Games);
        await CreateTransaction(child.Id, 10m, TransactionCategory.Toys);

        // Act
        var result = await _categoryService.GetCategorySpendingAsync(child.Id);

        // Assert
        result.Should().HaveCount(2);
        result.First().Category.Should().Be(TransactionCategory.Games);
        result.First().TotalAmount.Should().Be(20m);
        result.First().Percentage.Should().Be(50m); // 20/40 * 100
        result.Last().TotalAmount.Should().Be(20m); // 10 + 10
    }

    [Fact]
    public async Task GetCategorySpending_WithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var child = await CreateChild();

        // Act
        var result = await _categoryService.GetCategorySpendingAsync(child.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategorySpending_OnlyIncludesDebits_IgnoresCredits()
    {
        // Arrange
        var child = await CreateChild();
        await CreateCreditTransaction(child.Id, 50m, TransactionCategory.Allowance);
        await CreateDebitTransaction(child.Id, 10m, TransactionCategory.Toys);

        // Act
        var result = await _categoryService.GetCategorySpendingAsync(child.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().Category.Should().Be(TransactionCategory.Toys);
    }

    // Budget Status Tests
    [Fact]
    public async Task GetBudgetStatus_WithSpendingUnderLimit_ReturnsCorrectStatus()
    {
        // Arrange
        var child = await CreateChild();
        await CreateBudget(child.Id, TransactionCategory.Toys, 100m, BudgetPeriod.Weekly);
        await CreateDebitTransaction(child.Id, 50m, TransactionCategory.Toys);

        // Act
        var result = await _categoryService.GetBudgetStatusAsync(child.Id, BudgetPeriod.Weekly);

        // Assert
        var status = result.First();
        status.Category.Should().Be(TransactionCategory.Toys);
        status.CurrentSpending.Should().Be(50m);
        status.Remaining.Should().Be(50m);
        status.PercentUsed.Should().Be(50);
        status.Status.Should().Be(BudgetStatus.Safe);
    }

    [Fact]
    public async Task GetBudgetStatus_WithSpendingOverLimit_ReturnsOverBudgetStatus()
    {
        // Arrange
        var child = await CreateChild();
        await CreateBudget(child.Id, TransactionCategory.Snacks, 20m, BudgetPeriod.Weekly);
        await CreateDebitTransaction(child.Id, 25m, TransactionCategory.Snacks);

        // Act
        var result = await _categoryService.GetBudgetStatusAsync(child.Id, BudgetPeriod.Weekly);

        // Assert
        var status = result.First();
        status.Status.Should().Be(BudgetStatus.OverBudget);
        status.PercentUsed.Should().Be(125);
        status.Remaining.Should().Be(-5m);
    }

    // Budget Check Tests
    [Fact]
    public async Task CheckBudget_WithEnforcedLimit_PreventsBudgetExceeding()
    {
        // Arrange
        var child = await CreateChild();
        await CreateBudget(child.Id, TransactionCategory.Candy, 10m, BudgetPeriod.Weekly, enforceLimit: true);
        await CreateDebitTransaction(child.Id, 8m, TransactionCategory.Candy);

        // Act
        var result = await _categoryService.CheckBudgetAsync(child.Id, TransactionCategory.Candy, 5m);

        // Assert
        result.Allowed.Should().BeFalse();
        result.Message.Should().Contain("exceeds budget");
        result.CurrentSpending.Should().Be(8m);
        result.RemainingAfter.Should().Be(-3m);
    }

    [Fact]
    public async Task CheckBudget_WithoutEnforcedLimit_AllowsTransaction()
    {
        // Arrange
        var child = await CreateChild();
        await CreateBudget(child.Id, TransactionCategory.Toys, 50m, BudgetPeriod.Weekly, enforceLimit: false);
        await CreateDebitTransaction(child.Id, 45m, TransactionCategory.Toys);

        // Act
        var result = await _categoryService.CheckBudgetAsync(child.Id, TransactionCategory.Toys, 10m);

        // Assert
        result.Allowed.Should().BeTrue();
        result.Message.Should().Contain("No budget limit");
    }

    [Fact]
    public async Task CheckBudget_WithNoBudgetSet_AllowsTransaction()
    {
        // Arrange
        var child = await CreateChild();

        // Act
        var result = await _categoryService.CheckBudgetAsync(child.Id, TransactionCategory.Games, 100m);

        // Assert
        result.Allowed.Should().BeTrue();
    }

    // Category Suggestion Tests
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

    // Get Categories for Type Tests
    [Fact]
    public void GetCategoriesForType_Credit_ReturnsIncomeCategories()
    {
        // Act
        var result = _categoryService.GetCategoriesForType(TransactionType.Credit);

        // Assert
        result.Should().Contain(TransactionCategory.Allowance);
        result.Should().Contain(TransactionCategory.Chores);
        result.Should().NotContain(TransactionCategory.Toys);
    }

    [Fact]
    public void GetCategoriesForType_Debit_ReturnsSpendingCategories()
    {
        // Act
        var result = _categoryService.GetCategoriesForType(TransactionType.Debit);

        // Assert
        result.Should().Contain(TransactionCategory.Toys);
        result.Should().Contain(TransactionCategory.Games);
        result.Should().NotContain(TransactionCategory.Allowance);
    }
}
```

---

## Phase 3: Update TransactionService (TDD)

### 3.1 Enhanced TransactionService

Update the existing `TransactionService` to check budgets before creating transactions.

```csharp
public class TransactionService : ITransactionService
{
    private readonly AllowanceContext _context;
    private readonly ICategoryService _categoryService;
    private readonly IHubContext<FamilyHub>? _hubContext;

    public TransactionService(
        AllowanceContext context,
        ICategoryService categoryService,
        IHubContext<FamilyHub>? hubContext = null)
    {
        _context = context;
        _categoryService = categoryService;
        _hubContext = hubContext;
    }

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
    {
        // Validate child exists
        var child = await _context.Children.FindAsync(dto.ChildId)
            ?? throw new NotFoundException("Child not found");

        // Check budget if this is a debit transaction
        if (dto.Type == TransactionType.Debit)
        {
            var budgetCheck = await _categoryService.CheckBudgetAsync(
                dto.ChildId,
                dto.Category,
                dto.Amount);

            if (!budgetCheck.Allowed)
            {
                throw new BudgetExceededException(
                    $"Transaction would exceed budget for {dto.Category}. " +
                    $"Current: ${budgetCheck.CurrentSpending:F2}, " +
                    $"Limit: ${budgetCheck.BudgetLimit:F2}");
            }
        }

        // Rest of transaction creation logic...
    }
}
```

### 3.2 Additional Tests (5 Tests)

```csharp
[Fact]
public async Task CreateTransaction_WithEnforcedBudget_ThrowsWhenExceeding()
{
    // Arrange
    var child = await CreateChild(balance: 100m);
    await CreateBudget(child.Id, TransactionCategory.Toys, 20m, enforceLimit: true);
    await CreateDebitTransaction(child.Id, 18m, TransactionCategory.Toys);

    var dto = new CreateTransactionDto(
        child.Id,
        5m,
        TransactionType.Debit,
        TransactionCategory.Toys,
        "Another toy");

    // Act & Assert
    await Assert.ThrowsAsync<BudgetExceededException>(
        () => _transactionService.CreateTransactionAsync(dto));
}

[Fact]
public async Task CreateTransaction_WithoutEnforcedBudget_AllowsOverage()
{
    // Similar test but with enforceLimit: false
    // Should succeed
}
```

---

## Phase 4: API Controllers

### 4.1 CategoriesController

```csharp
[ApiController]
[Route("api/v1/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ICurrentUserService _currentUserService;

    [HttpGet("spending/{childId}")]
    public async Task<ActionResult<List<CategorySpendingDto>>> GetCategorySpending(
        Guid childId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _categoryService.GetCategorySpendingAsync(
            childId,
            startDate,
            endDate);

        return Ok(result);
    }

    [HttpGet("budget-status/{childId}")]
    public async Task<ActionResult<List<CategoryBudgetStatusDto>>> GetBudgetStatus(
        Guid childId,
        [FromQuery] BudgetPeriod period = BudgetPeriod.Weekly)
    {
        var result = await _categoryService.GetBudgetStatusAsync(childId, period);
        return Ok(result);
    }

    [HttpGet("suggest")]
    public ActionResult<TransactionCategory> SuggestCategory(
        [FromQuery] string description,
        [FromQuery] TransactionType type)
    {
        var result = _categoryService.SuggestCategory(description, type);
        return Ok(new { Category = result, CategoryName = result.ToString() });
    }

    [HttpGet("list")]
    public ActionResult<List<CategoryOption>> GetCategories(
        [FromQuery] TransactionType type)
    {
        var categories = _categoryService.GetCategoriesForType(type);
        var result = categories.Select(c => new CategoryOption(
            (int)c,
            c.ToString(),
            GetCategoryIcon(c)
        )).ToList();

        return Ok(result);
    }

    private string GetCategoryIcon(TransactionCategory category)
    {
        // Return emoji or icon name for category
        return category switch
        {
            TransactionCategory.Toys => "🧸",
            TransactionCategory.Games => "🎮",
            TransactionCategory.Books => "📚",
            TransactionCategory.Snacks => "🍿",
            TransactionCategory.Candy => "🍬",
            _ => "💰"
        };
    }
}

public record CategoryOption(int Value, string Label, string Icon);
```

### 4.2 CategoryBudgetsController

```csharp
[ApiController]
[Route("api/v1/budgets")]
[Authorize(Roles = "Parent")]
public class CategoryBudgetsController : ControllerBase
{
    private readonly ICategoryBudgetService _budgetService;
    private readonly ICurrentUserService _currentUserService;

    [HttpPost]
    public async Task<ActionResult<CategoryBudget>> SetBudget(SetBudgetDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var result = await _budgetService.SetBudgetAsync(dto, userId);
        return CreatedAtAction(nameof(GetBudget), new { id = result.Id }, result);
    }

    [HttpGet("{childId}")]
    public async Task<ActionResult<List<CategoryBudget>>> GetBudgets(Guid childId)
    {
        var result = await _budgetService.GetAllBudgetsAsync(childId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryBudget>> GetBudget(Guid id)
    {
        // Implementation
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBudget(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        await _budgetService.DeleteBudgetAsync(id, userId);
        return NoContent();
    }
}
```

---

## Phase 5: Blazor UI Components

### 5.1 CategoryPieChart Component

```razor
@using ApexCharts
@inject ICategoryService CategoryService

<div class="category-pie-chart">
    <div class="chart-header">
        <h5>Spending by Category</h5>
        <div class="date-range-selector">
            <button class="btn btn-sm" @onclick="() => LoadData(7)">7D</button>
            <button class="btn btn-sm" @onclick="() => LoadData(30)">30D</button>
            <button class="btn btn-sm" @onclick="() => LoadData(90)">90D</button>
        </div>
    </div>

    @if (Loading)
    {
        <div class="spinner-border"></div>
    }
    else if (Data.Any())
    {
        <ApexChart TItem="CategorySpendingDto"
                   Title="Spending Breakdown"
                   Options="@chartOptions">
            <ApexPointSeries TItem="CategorySpendingDto"
                            Items="@Data"
                            SeriesType="SeriesType.Pie"
                            Name="Amount"
                            XValue="@(e => e.CategoryName)"
                            YValue="@(e => (decimal?)e.TotalAmount)" />
        </ApexChart>

        <div class="category-legend mt-3">
            @foreach (var item in Data)
            {
                <div class="legend-item">
                    <span class="color-box" style="background-color: @GetColor(item.Category)"></span>
                    <span class="category-name">@item.CategoryName</span>
                    <span class="category-amount">@item.TotalAmount.ToString("C")</span>
                    <span class="category-percent">(@item.Percentage.ToString("F1")%)</span>
                </div>
            }
        </div>
    }
    else
    {
        <p class="text-muted">No spending data yet.</p>
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }

    private List<CategorySpendingDto> Data = new();
    private bool Loading = true;

    private ApexChartOptions<CategorySpendingDto> chartOptions = new()
    {
        Chart = new Chart { Type = ChartType.Pie },
        Legend = new Legend { Position = LegendPosition.Bottom },
        Colors = new List<string>
        {
            "#10B981", "#3B82F6", "#F59E0B", "#EF4444",
            "#8B5CF6", "#EC4899", "#14B8A6", "#F97316"
        }
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadData(30);
    }

    private async Task LoadData(int days)
    {
        Loading = true;
        var startDate = DateTime.UtcNow.AddDays(-days);
        Data = await CategoryService.GetCategorySpendingAsync(ChildId, startDate);
        Loading = false;
    }

    private string GetColor(TransactionCategory category)
    {
        // Return consistent color per category
        return category switch
        {
            TransactionCategory.Toys => "#10B981",
            TransactionCategory.Games => "#3B82F6",
            TransactionCategory.Snacks => "#F59E0B",
            _ => "#6B7280"
        };
    }
}
```

### 5.2 BudgetStatusWidget Component

```razor
@inject ICategoryService CategoryService

<div class="budget-status-widget">
    <h5>Budget Status</h5>

    @if (BudgetStatuses.Any())
    {
        @foreach (var status in BudgetStatuses)
        {
            <div class="budget-item mb-3">
                <div class="d-flex justify-content-between mb-1">
                    <span class="category-name">
                        @GetCategoryIcon(status.Category) @status.CategoryName
                    </span>
                    <span class="budget-amounts">
                        @status.CurrentSpending.ToString("C") / @status.BudgetLimit.ToString("C")
                    </span>
                </div>

                <div class="progress" style="height: 24px;">
                    <div class="progress-bar @GetProgressBarClass(status.Status)"
                         role="progressbar"
                         style="width: @Math.Min(status.PercentUsed, 100)%"
                         aria-valuenow="@status.PercentUsed"
                         aria-valuemin="0"
                         aria-valuemax="100">
                        @status.PercentUsed%
                    </div>
                </div>

                @if (status.Status == BudgetStatus.Warning || status.Status == BudgetStatus.OverBudget)
                {
                    <small class="text-@(status.Status == BudgetStatus.OverBudget ? "danger" : "warning")">
                        @if (status.Remaining < 0)
                        {
                            <span>⚠️ Over budget by @Math.Abs(status.Remaining).ToString("C")</span>
                        }
                        else
                        {
                            <span>⚠️ Only @status.Remaining.ToString("C") remaining</span>
                        }
                    </small>
                }
            </div>
        }
    }
    else
    {
        <p class="text-muted">No budgets set yet.</p>
        <button class="btn btn-sm btn-primary" @onclick="OnAddBudgetClick">
            Add Budget
        </button>
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }
    [Parameter] public BudgetPeriod Period { get; set; } = BudgetPeriod.Weekly;
    [Parameter] public EventCallback OnAddBudgetClick { get; set; }

    private List<CategoryBudgetStatusDto> BudgetStatuses = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        BudgetStatuses = await CategoryService.GetBudgetStatusAsync(ChildId, Period);
    }

    private string GetProgressBarClass(BudgetStatus status)
    {
        return status switch
        {
            BudgetStatus.Safe => "bg-success",
            BudgetStatus.Warning => "bg-warning",
            BudgetStatus.AtLimit => "bg-danger",
            BudgetStatus.OverBudget => "bg-danger",
            _ => "bg-secondary"
        };
    }

    private string GetCategoryIcon(TransactionCategory category)
    {
        return category switch
        {
            TransactionCategory.Toys => "🧸",
            TransactionCategory.Games => "🎮",
            TransactionCategory.Books => "📚",
            TransactionCategory.Snacks => "🍿",
            TransactionCategory.Candy => "🍬",
            _ => "💰"
        };
    }
}
```

### 5.3 Enhanced TransactionForm Component

Update the existing `TransactionForm` to include category selection:

```razor
<div class="mb-3">
    <label for="category" class="form-label">Category</label>
    <select id="category" class="form-select" @bind="SelectedCategory">
        @foreach (var category in AvailableCategories)
        {
            <option value="@category">@GetCategoryIcon(category) @category.ToString()</option>
        }
    </select>
</div>

@code {
    private TransactionCategory SelectedCategory;
    private List<TransactionCategory> AvailableCategories = new();

    protected override void OnParametersSet()
    {
        AvailableCategories = CategoryService.GetCategoriesForType(TransactionType);
        SelectedCategory = CategoryService.SuggestCategory(Description, TransactionType);
    }
}
```

### 5.4 Component Tests (8 bUnit Tests)

```csharp
public class CategoryPieChartTests : TestContext
{
    [Fact]
    public void CategoryPieChart_RendersChart_WithData()
    {
        // Arrange
        var mockService = new Mock<ICategoryService>();
        mockService.Setup(s => s.GetCategorySpendingAsync(It.IsAny<Guid>(), null, null))
            .ReturnsAsync(new List<CategorySpendingDto>
            {
                new(TransactionCategory.Toys, "Toys", 50m, 5, 50m),
                new(TransactionCategory.Games, "Games", 50m, 3, 50m)
            });
        Services.AddSingleton(mockService.Object);

        // Act
        var cut = RenderComponent<CategoryPieChart>(parameters => parameters
            .Add(p => p.ChildId, Guid.NewGuid()));

        // Assert
        cut.Find(".category-pie-chart").Should().NotBeNull();
        cut.FindAll(".legend-item").Should().HaveCount(2);
    }

    // Additional component tests...
}
```

---

## Phase 6: Dashboard Integration

Add the new components to the existing Dashboard page:

```razor
@page "/dashboard"

<!-- Existing child cards -->
<div class="row">
    @foreach (var child in Children)
    {
        <div class="col-lg-6 mb-4">
            <div class="card">
                <div class="card-body">
                    <ChildCard Child="@child" />

                    <!-- NEW: Category widgets -->
                    <div class="mt-4">
                        <CategoryPieChart ChildId="@child.Id" />
                    </div>

                    <div class="mt-4">
                        <BudgetStatusWidget ChildId="@child.Id" Period="BudgetPeriod.Weekly" />
                    </div>
                </div>
            </div>
        </div>
    }
</div>
```

---

## Testing Summary

### Total Tests: 25

**Service Tests (12)**:
- CategoryService: GetCategorySpending (3)
- CategoryService: GetBudgetStatus (2)
- CategoryService: CheckBudget (3)
- CategoryService: SuggestCategory (2)
- CategoryService: GetCategoriesForType (2)

**Integration Tests (5)**:
- TransactionService with budget enforcement (5)

**Component Tests (8)**:
- CategoryPieChart (4)
- BudgetStatusWidget (4)

---

## Success Metrics

- ✅ All 25 tests passing
- ✅ Categories visible on all transactions
- ✅ Budget enforcement working correctly
- ✅ Pie chart renders smoothly
- ✅ Budget widget shows real-time status
- ✅ API endpoints functional

---

## Future Enhancements

1. **Custom Categories**: Allow parents to create custom categories
2. **Budget Templates**: Pre-defined budget templates by age group
3. **Category Goals**: "Try to spend less than $X on candy this month"
4. **Spending Insights**: AI-powered suggestions ("You spend 40% on toys")
5. **Export by Category**: CSV export filtered by category

---

**Next Steps**: Implement this spec following TDD methodology!
