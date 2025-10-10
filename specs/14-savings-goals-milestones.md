# Savings Goals & Milestones Specification

## Overview

This specification enhances the existing WishListItem model with milestone tracking, multiple savings buckets, automatic transfers, and goal achievement rewards. It teaches children to save systematically toward specific goals while celebrating progress along the way.

## Goals

1. **Milestone Motivation**: Break large goals into achievable milestones (25%, 50%, 75%, 100%)
2. **Automatic Savings**: Transfer X% of allowance to specific goals automatically
3. **Multiple Buckets**: Children manage multiple savings goals simultaneously
4. **Progress Visualization**: Visual progress bars and completion celebrations
5. **Achievement Rewards**: Unlock badges and bonuses upon goal completion
6. **TDD Approach**: 30 comprehensive tests

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Entity Framework Core
- **Database**: PostgreSQL with JSON column support
- **Testing**: xUnit, FluentAssertions, Moq
- **Charts**: ApexCharts for progress visualization
- **UI**: Blazor Server with real-time updates

---

## Phase 1: Database Schema Enhancement

### 1.1 Enhanced WishListItem Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Savings goal with milestone tracking and auto-transfer support
/// </summary>
public class WishListItem
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Target amount to save
    /// </summary>
    public decimal TargetAmount { get; set; }

    /// <summary>
    /// Amount currently saved toward this goal
    /// </summary>
    public decimal CurrentAmount { get; set; } = 0;

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    /// <summary>
    /// Priority ranking (1 = highest)
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Target completion date (optional)
    /// </summary>
    public DateTime? TargetDate { get; set; }

    /// <summary>
    /// Date when goal was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Is this goal currently active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Percentage of allowance to auto-transfer (0-100)
    /// </summary>
    public int AutoTransferPercent { get; set; } = 0;

    /// <summary>
    /// Image URL for the desired item
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Product URL for purchase link
    /// </summary>
    public string? ProductUrl { get; set; }

    /// <summary>
    /// Notes about the goal
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<SavingsGoalMilestone> Milestones { get; set; } = new List<SavingsGoalMilestone>();
    public virtual ICollection<SavingsGoalTransaction> Transactions { get; set; } = new List<SavingsGoalTransaction>();
}
```

### 1.2 SavingsGoalMilestone Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Tracks milestone achievements for savings goals
/// </summary>
public class SavingsGoalMilestone
{
    public Guid Id { get; set; }

    public Guid WishListItemId { get; set; }
    public virtual WishListItem WishListItem { get; set; } = null!;

    /// <summary>
    /// Milestone percentage (25, 50, 75, 100)
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// Dollar amount at this milestone
    /// </summary>
    public decimal TargetAmount { get; set; }

    /// <summary>
    /// When milestone was reached
    /// </summary>
    public DateTime? AchievedAt { get; set; }

    /// <summary>
    /// Is this milestone reached?
    /// </summary>
    public bool IsAchieved { get; set; } = false;

    /// <summary>
    /// Reward amount for reaching milestone (optional)
    /// </summary>
    public decimal? RewardAmount { get; set; }

    /// <summary>
    /// Achievement earned (optional)
    /// </summary>
    public Guid? AchievementId { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

### 1.3 SavingsGoalTransaction Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Tracks individual transfers to/from savings goals
/// </summary>
public class SavingsGoalTransaction
{
    public Guid Id { get; set; }

    public Guid WishListItemId { get; set; }
    public virtual WishListItem WishListItem { get; set; } = null!;

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    /// <summary>
    /// Amount transferred (positive = deposit, negative = withdrawal)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Balance in goal after this transaction
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Transaction type
    /// </summary>
    public SavingsTransactionType Type { get; set; }

    /// <summary>
    /// Description of transfer
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Was this an automatic transfer?
    /// </summary>
    public bool IsAutomatic { get; set; } = false;

    /// <summary>
    /// Related main transaction (if transfer from allowance)
    /// </summary>
    public Guid? SourceTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}

public enum SavingsTransactionType
{
    Deposit = 1,        // Money added to goal
    Withdrawal = 2,     // Money removed from goal
    AutoTransfer = 3,   // Automatic transfer from allowance
    MilestoneReward = 4,// Bonus for reaching milestone
    GoalComplete = 5    // Final completion bonus
}
```

### 1.4 Update Child Model

```csharp
namespace AllowanceTracker.Models;

public class Child
{
    // ... existing properties ...

    /// <summary>
    /// Total saved across all active goals
    /// </summary>
    public decimal TotalGoalSavings { get; set; } = 0;

    /// <summary>
    /// Number of completed goals (lifetime)
    /// </summary>
    public int CompletedGoalsCount { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<WishListItem> WishListItems { get; set; } = new List<WishListItem>();
}
```

### 1.5 Database Migration

```bash
dotnet ef migrations add AddSavingsGoalsMilestones
```

```csharp
// Migration code
public partial class AddSavingsGoalsMilestones : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Enhance WishListItems table
        migrationBuilder.AddColumn<decimal>(
            name: "CurrentAmount",
            table: "WishListItems",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            name: "AutoTransferPercent",
            table: "WishListItems",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "CompletedAt",
            table: "WishListItems",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ImageUrl",
            table: "WishListItems",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProductUrl",
            table: "WishListItems",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Notes",
            table: "WishListItems",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAt",
            table: "WishListItems",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: DateTime.UtcNow);

        // Create SavingsGoalMilestones table
        migrationBuilder.CreateTable(
            name: "SavingsGoalMilestones",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WishListItemId = table.Column<Guid>(type: "uuid", nullable: false),
                PercentComplete = table.Column<int>(type: "integer", nullable: false),
                TargetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                AchievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsAchieved = table.Column<bool>(type: "boolean", nullable: false),
                RewardAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                AchievementId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavingsGoalMilestones", x => x.Id);
                table.ForeignKey(
                    name: "FK_SavingsGoalMilestones_WishListItems_WishListItemId",
                    column: x => x.WishListItemId,
                    principalTable: "WishListItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create SavingsGoalTransactions table
        migrationBuilder.CreateTable(
            name: "SavingsGoalTransactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WishListItemId = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                SourceTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavingsGoalTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_SavingsGoalTransactions_WishListItems_WishListItemId",
                    column: x => x.WishListItemId,
                    principalTable: "WishListItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SavingsGoalTransactions_Children_ChildId",
                    column: x => x.ChildId,
                    principalTable: "Children",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SavingsGoalTransactions_AspNetUsers_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Add indexes
        migrationBuilder.CreateIndex(
            name: "IX_SavingsGoalMilestones_WishListItemId",
            table: "SavingsGoalMilestones",
            column: "WishListItemId");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsGoalTransactions_WishListItemId",
            table: "SavingsGoalTransactions",
            column: "WishListItemId");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsGoalTransactions_ChildId",
            table: "SavingsGoalTransactions",
            column: "ChildId");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsGoalTransactions_CreatedAt",
            table: "SavingsGoalTransactions",
            column: "CreatedAt");

        // Update Children table
        migrationBuilder.AddColumn<decimal>(
            name: "TotalGoalSavings",
            table: "Children",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            name: "CompletedGoalsCount",
            table: "Children",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
```

---

## Phase 2: Service Layer (TDD)

### 2.1 ISavingsGoalService Interface

```csharp
namespace AllowanceTracker.Services;

public interface ISavingsGoalService
{
    // Goal Management
    Task<WishListItem> CreateGoalAsync(CreateSavingsGoalDto dto, Guid currentUserId);
    Task<WishListItem> GetGoalAsync(Guid goalId);
    Task<List<WishListItem>> GetActiveGoalsAsync(Guid childId);
    Task<List<WishListItem>> GetCompletedGoalsAsync(Guid childId);
    Task<WishListItem> UpdateGoalAsync(Guid goalId, UpdateSavingsGoalDto dto, Guid currentUserId);
    Task DeleteGoalAsync(Guid goalId, Guid currentUserId);

    // Savings Transactions
    Task<SavingsGoalTransaction> DepositToGoalAsync(Guid goalId, decimal amount, string description, Guid userId);
    Task<SavingsGoalTransaction> WithdrawFromGoalAsync(Guid goalId, decimal amount, string description, Guid userId);
    Task ProcessAutoTransfersAsync(Guid childId, Guid transactionId, decimal allowanceAmount);

    // Milestone Management
    Task<List<SavingsGoalMilestone>> CreateMilestonesForGoalAsync(Guid goalId);
    Task CheckAndAwardMilestonesAsync(Guid goalId);
    Task<List<SavingsGoalMilestone>> GetMilestonesAsync(Guid goalId);

    // Goal Completion
    Task<WishListItem> CompleteGoalAsync(Guid goalId, Guid userId);
    Task<WishListItem> ReactivateGoalAsync(Guid goalId, Guid userId);

    // Analytics
    Task<SavingsGoalProgressDto> GetProgressAsync(Guid goalId);
    Task<SavingsGoalStatistics> GetStatisticsAsync(Guid childId);
    Task<List<SavingsGoalTransaction>> GetTransactionHistoryAsync(Guid goalId);

    // Estimates
    decimal CalculateWeeksToGoal(decimal currentAmount, decimal targetAmount, decimal weeklyAllowance, int autoTransferPercent);
    DateTime? EstimateCompletionDate(Guid goalId, decimal weeklyAllowance);
}
```

### 2.2 Data Transfer Objects

```csharp
namespace AllowanceTracker.DTOs;

public record CreateSavingsGoalDto(
    Guid ChildId,
    string Name,
    string Description,
    decimal TargetAmount,
    DateTime? TargetDate = null,
    int AutoTransferPercent = 0,
    int Priority = 1,
    string? ImageUrl = null,
    string? ProductUrl = null,
    string? Notes = null);

public record UpdateSavingsGoalDto(
    string? Name = null,
    string? Description = null,
    decimal? TargetAmount = null,
    DateTime? TargetDate = null,
    int? AutoTransferPercent = null,
    int? Priority = null,
    string? ImageUrl = null,
    string? ProductUrl = null,
    string? Notes = null,
    bool? IsActive = null);

public record SavingsGoalProgressDto(
    Guid GoalId,
    string Name,
    decimal CurrentAmount,
    decimal TargetAmount,
    decimal PercentComplete,
    decimal AmountRemaining,
    int DaysActive,
    DateTime? EstimatedCompletionDate,
    int WeeksToCompletion,
    List<MilestoneProgressDto> Milestones);

public record MilestoneProgressDto(
    int PercentComplete,
    decimal TargetAmount,
    bool IsAchieved,
    DateTime? AchievedAt);

public record SavingsGoalStatistics(
    int TotalGoals,
    int ActiveGoals,
    int CompletedGoals,
    decimal TotalSaved,
    decimal TotalTargetAmount,
    double OverallCompletionRate,
    int TotalMilestonesAchieved,
    decimal AverageGoalAmount,
    int AverageDaysToComplete);
```

### 2.3 SavingsGoalService Implementation

```csharp
namespace AllowanceTracker.Services;

public class SavingsGoalService : ISavingsGoalService
{
    private readonly AllowanceContext _context;
    private readonly IHubContext<FamilyHub>? _hubContext;
    private readonly ILogger<SavingsGoalService> _logger;

    public SavingsGoalService(
        AllowanceContext context,
        IHubContext<FamilyHub>? hubContext,
        ILogger<SavingsGoalService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<WishListItem> CreateGoalAsync(CreateSavingsGoalDto dto, Guid currentUserId)
    {
        // Validate child exists
        var child = await _context.Children.FindAsync(dto.ChildId)
            ?? throw new NotFoundException("Child not found");

        // Validate target amount
        if (dto.TargetAmount <= 0)
            throw new ValidationException("Target amount must be greater than zero");

        // Validate auto-transfer percentage
        if (dto.AutoTransferPercent < 0 || dto.AutoTransferPercent > 100)
            throw new ValidationException("Auto-transfer percent must be between 0 and 100");

        var goal = new WishListItem
        {
            Id = Guid.NewGuid(),
            ChildId = dto.ChildId,
            Name = dto.Name,
            Description = dto.Description,
            TargetAmount = dto.TargetAmount,
            CurrentAmount = 0,
            TargetDate = dto.TargetDate,
            AutoTransferPercent = dto.AutoTransferPercent,
            Priority = dto.Priority,
            ImageUrl = dto.ImageUrl,
            ProductUrl = dto.ProductUrl,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WishListItems.Add(goal);
        await _context.SaveChangesAsync();

        // Create default milestones (25%, 50%, 75%, 100%)
        await CreateMilestonesForGoalAsync(goal.Id);

        _logger.LogInformation("Created savings goal {GoalId} for child {ChildId}", goal.Id, dto.ChildId);

        return goal;
    }

    public async Task<List<SavingsGoalMilestone>> CreateMilestonesForGoalAsync(Guid goalId)
    {
        var goal = await _context.WishListItems.FindAsync(goalId)
            ?? throw new NotFoundException("Goal not found");

        var milestones = new List<SavingsGoalMilestone>();
        var milestonePercentages = new[] { 25, 50, 75, 100 };

        foreach (var percent in milestonePercentages)
        {
            var milestone = new SavingsGoalMilestone
            {
                Id = Guid.NewGuid(),
                WishListItemId = goalId,
                PercentComplete = percent,
                TargetAmount = (goal.TargetAmount * percent) / 100,
                IsAchieved = false,
                CreatedAt = DateTime.UtcNow
            };

            milestones.Add(milestone);
            _context.SavingsGoalMilestones.Add(milestone);
        }

        await _context.SaveChangesAsync();
        return milestones;
    }

    public async Task<SavingsGoalTransaction> DepositToGoalAsync(
        Guid goalId,
        decimal amount,
        string description,
        Guid userId)
    {
        if (amount <= 0)
            throw new ValidationException("Deposit amount must be greater than zero");

        var goal = await _context.WishListItems
            .Include(g => g.Child)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new NotFoundException("Goal not found");

        if (!goal.IsActive)
            throw new InvalidOperationException("Cannot deposit to inactive goal");

        // Check if child has sufficient balance
        if (goal.Child.CurrentBalance < amount)
            throw new InsufficientFundsException($"Insufficient balance. Available: {goal.Child.CurrentBalance:C}");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Deduct from child's balance
            goal.Child.CurrentBalance -= amount;

            // Add to goal
            goal.CurrentAmount += amount;
            goal.Child.TotalGoalSavings += amount;
            goal.UpdatedAt = DateTime.UtcNow;

            // Create savings transaction record
            var savingsTransaction = new SavingsGoalTransaction
            {
                Id = Guid.NewGuid(),
                WishListItemId = goalId,
                ChildId = goal.ChildId,
                Amount = amount,
                BalanceAfter = goal.CurrentAmount,
                Type = SavingsTransactionType.Deposit,
                Description = description,
                IsAutomatic = false,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            _context.SavingsGoalTransactions.Add(savingsTransaction);
            await _context.SaveChangesAsync();

            // Check for milestone achievements
            await CheckAndAwardMilestonesAsync(goalId);

            // Check if goal is complete
            if (goal.CurrentAmount >= goal.TargetAmount && goal.CompletedAt == null)
            {
                await CompleteGoalAsync(goalId, userId);
            }

            await transaction.CommitAsync();

            // Send real-time update
            await _hubContext?.Clients
                .Group($"family-{goal.Child.FamilyId}")
                .SendAsync("SavingsGoalUpdated", goalId, goal.CurrentAmount, goal.TargetAmount);

            _logger.LogInformation("Deposited {Amount} to goal {GoalId}", amount, goalId);

            return savingsTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SavingsGoalTransaction> WithdrawFromGoalAsync(
        Guid goalId,
        decimal amount,
        string description,
        Guid userId)
    {
        if (amount <= 0)
            throw new ValidationException("Withdrawal amount must be greater than zero");

        var goal = await _context.WishListItems
            .Include(g => g.Child)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new NotFoundException("Goal not found");

        if (goal.CurrentAmount < amount)
            throw new InsufficientFundsException($"Insufficient goal balance. Available: {goal.CurrentAmount:C}");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove from goal
            goal.CurrentAmount -= amount;
            goal.Child.TotalGoalSavings -= amount;
            goal.UpdatedAt = DateTime.UtcNow;

            // Add back to child's balance
            goal.Child.CurrentBalance += amount;

            // Create savings transaction record
            var savingsTransaction = new SavingsGoalTransaction
            {
                Id = Guid.NewGuid(),
                WishListItemId = goalId,
                ChildId = goal.ChildId,
                Amount = -amount, // Negative for withdrawal
                BalanceAfter = goal.CurrentAmount,
                Type = SavingsTransactionType.Withdrawal,
                Description = description,
                IsAutomatic = false,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            _context.SavingsGoalTransactions.Add(savingsTransaction);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Send real-time update
            await _hubContext?.Clients
                .Group($"family-{goal.Child.FamilyId}")
                .SendAsync("SavingsGoalUpdated", goalId, goal.CurrentAmount, goal.TargetAmount);

            _logger.LogInformation("Withdrew {Amount} from goal {GoalId}", amount, goalId);

            return savingsTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ProcessAutoTransfersAsync(Guid childId, Guid transactionId, decimal allowanceAmount)
    {
        var activeGoals = await _context.WishListItems
            .Where(g => g.ChildId == childId && g.IsActive && g.AutoTransferPercent > 0)
            .OrderBy(g => g.Priority)
            .ToListAsync();

        if (!activeGoals.Any())
            return;

        var child = await _context.Children.FindAsync(childId)
            ?? throw new NotFoundException("Child not found");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var goal in activeGoals)
            {
                var transferAmount = Math.Round((allowanceAmount * goal.AutoTransferPercent) / 100, 2);

                if (transferAmount > 0 && child.CurrentBalance >= transferAmount)
                {
                    // Deduct from balance
                    child.CurrentBalance -= transferAmount;

                    // Add to goal
                    goal.CurrentAmount += transferAmount;
                    child.TotalGoalSavings += transferAmount;
                    goal.UpdatedAt = DateTime.UtcNow;

                    // Create auto-transfer record
                    var savingsTransaction = new SavingsGoalTransaction
                    {
                        Id = Guid.NewGuid(),
                        WishListItemId = goal.Id,
                        ChildId = childId,
                        Amount = transferAmount,
                        BalanceAfter = goal.CurrentAmount,
                        Type = SavingsTransactionType.AutoTransfer,
                        Description = $"Auto-transfer {goal.AutoTransferPercent}% from allowance",
                        IsAutomatic = true,
                        SourceTransactionId = transactionId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedById = child.UserId
                    };

                    _context.SavingsGoalTransactions.Add(savingsTransaction);

                    // Check milestones
                    await CheckAndAwardMilestonesAsync(goal.Id);

                    // Check completion
                    if (goal.CurrentAmount >= goal.TargetAmount && goal.CompletedAt == null)
                    {
                        await CompleteGoalAsync(goal.Id, child.UserId);
                    }

                    _logger.LogInformation(
                        "Auto-transferred {Amount} to goal {GoalId} for child {ChildId}",
                        transferAmount, goal.Id, childId);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task CheckAndAwardMilestonesAsync(Guid goalId)
    {
        var goal = await _context.WishListItems
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new NotFoundException("Goal not found");

        var percentComplete = goal.TargetAmount > 0
            ? (goal.CurrentAmount / goal.TargetAmount) * 100
            : 0;

        foreach (var milestone in goal.Milestones.Where(m => !m.IsAchieved))
        {
            if (percentComplete >= milestone.PercentComplete)
            {
                milestone.IsAchieved = true;
                milestone.AchievedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Milestone achieved: {Percent}% for goal {GoalId}",
                    milestone.PercentComplete, goalId);

                // Send celebration notification
                await _hubContext?.Clients
                    .Group($"family-{goal.Child.FamilyId}")
                    .SendAsync("MilestoneAchieved", goalId, milestone.PercentComplete);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<WishListItem> CompleteGoalAsync(Guid goalId, Guid userId)
    {
        var goal = await _context.WishListItems
            .Include(g => g.Child)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new NotFoundException("Goal not found");

        if (goal.CompletedAt != null)
            throw new InvalidOperationException("Goal already completed");

        goal.CompletedAt = DateTime.UtcNow;
        goal.IsActive = false;
        goal.UpdatedAt = DateTime.UtcNow;

        // Increment child's completed goals count
        goal.Child.CompletedGoalsCount++;

        await _context.SaveChangesAsync();

        // Send celebration
        await _hubContext?.Clients
            .Group($"family-{goal.Child.FamilyId}")
            .SendAsync("SavingsGoalCompleted", goalId, goal.Name, goal.TargetAmount);

        _logger.LogInformation("Goal {GoalId} completed for child {ChildId}", goalId, goal.ChildId);

        return goal;
    }

    public async Task<SavingsGoalProgressDto> GetProgressAsync(Guid goalId)
    {
        var goal = await _context.WishListItems
            .Include(g => g.Milestones)
            .Include(g => g.Child)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new NotFoundException("Goal not found");

        var percentComplete = goal.TargetAmount > 0
            ? (goal.CurrentAmount / goal.TargetAmount) * 100
            : 0;

        var amountRemaining = goal.TargetAmount - goal.CurrentAmount;
        var daysActive = (DateTime.UtcNow - goal.CreatedAt).Days;

        var weeksToCompletion = CalculateWeeksToGoal(
            goal.CurrentAmount,
            goal.TargetAmount,
            goal.Child.WeeklyAllowance,
            goal.AutoTransferPercent);

        DateTime? estimatedCompletion = null;
        if (weeksToCompletion > 0 && weeksToCompletion < 1000)
        {
            estimatedCompletion = DateTime.UtcNow.AddDays(weeksToCompletion * 7);
        }

        var milestones = goal.Milestones
            .OrderBy(m => m.PercentComplete)
            .Select(m => new MilestoneProgressDto(
                m.PercentComplete,
                m.TargetAmount,
                m.IsAchieved,
                m.AchievedAt))
            .ToList();

        return new SavingsGoalProgressDto(
            goal.Id,
            goal.Name,
            goal.CurrentAmount,
            goal.TargetAmount,
            Math.Round(percentComplete, 2),
            amountRemaining,
            daysActive,
            estimatedCompletion,
            weeksToCompletion,
            milestones);
    }

    public decimal CalculateWeeksToGoal(
        decimal currentAmount,
        decimal targetAmount,
        decimal weeklyAllowance,
        int autoTransferPercent)
    {
        var remaining = targetAmount - currentAmount;
        if (remaining <= 0) return 0;

        var weeklyContribution = (weeklyAllowance * autoTransferPercent) / 100;
        if (weeklyContribution <= 0) return int.MaxValue;

        return Math.Ceiling(remaining / weeklyContribution);
    }

    public async Task<SavingsGoalStatistics> GetStatisticsAsync(Guid childId)
    {
        var allGoals = await _context.WishListItems
            .Where(g => g.ChildId == childId)
            .ToListAsync();

        var activeGoals = allGoals.Where(g => g.IsActive).ToList();
        var completedGoals = allGoals.Where(g => g.CompletedAt != null).ToList();

        var totalSaved = allGoals.Sum(g => g.CurrentAmount);
        var totalTarget = activeGoals.Sum(g => g.TargetAmount);

        var totalMilestones = await _context.SavingsGoalMilestones
            .Where(m => allGoals.Select(g => g.Id).Contains(m.WishListItemId))
            .CountAsync(m => m.IsAchieved);

        var overallCompletion = allGoals.Any()
            ? allGoals.Average(g => g.TargetAmount > 0 ? (double)(g.CurrentAmount / g.TargetAmount) * 100 : 0)
            : 0;

        var avgGoalAmount = allGoals.Any() ? allGoals.Average(g => g.TargetAmount) : 0;

        var avgDaysToComplete = completedGoals.Any()
            ? (int)completedGoals.Average(g => (g.CompletedAt!.Value - g.CreatedAt).TotalDays)
            : 0;

        return new SavingsGoalStatistics(
            allGoals.Count,
            activeGoals.Count,
            completedGoals.Count,
            totalSaved,
            totalTarget,
            Math.Round(overallCompletion, 2),
            totalMilestones,
            avgGoalAmount,
            avgDaysToComplete);
    }
}
```

### 2.4 Update TransactionService

Add auto-transfer hook to existing TransactionService:

```csharp
public class TransactionService : ITransactionService
{
    private readonly ISavingsGoalService _savingsGoalService;

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
    {
        // ... existing transaction creation code ...

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // NEW: Process auto-transfers if this is allowance
        if (createdTransaction.Type == TransactionType.Credit &&
            createdTransaction.Category == TransactionCategory.Allowance)
        {
            await _savingsGoalService.ProcessAutoTransfersAsync(
                dto.ChildId,
                createdTransaction.Id,
                dto.Amount);
        }

        return createdTransaction;
    }
}
```

### 2.5 Test Cases (30 Tests)

```csharp
namespace AllowanceTracker.Tests.Services;

public class SavingsGoalServiceTests
{
    // Create Goal Tests (5)
    [Fact]
    public async Task CreateGoal_ValidData_CreatesSuccessfully()
    {
        // Arrange
        var child = await CreateChild();
        var dto = new CreateSavingsGoalDto(
            child.Id,
            "New Bicycle",
            "Mountain bike for trails",
            250m,
            TargetDate: DateTime.UtcNow.AddMonths(3),
            AutoTransferPercent: 20);

        // Act
        var goal = await _savingsGoalService.CreateGoalAsync(dto, _parentUserId);

        // Assert
        goal.Should().NotBeNull();
        goal.Name.Should().Be("New Bicycle");
        goal.TargetAmount.Should().Be(250m);
        goal.AutoTransferPercent.Should().Be(20);
        goal.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateGoal_AutomaticallyCreatesMilestones()
    {
        // Arrange
        var child = await CreateChild();
        var dto = new CreateSavingsGoalDto(child.Id, "Goal", "Test", 100m);

        // Act
        var goal = await _savingsGoalService.CreateGoalAsync(dto, _parentUserId);
        var milestones = await _savingsGoalService.GetMilestonesAsync(goal.Id);

        // Assert
        milestones.Should().HaveCount(4); // 25%, 50%, 75%, 100%
        milestones[0].PercentComplete.Should().Be(25);
        milestones[0].TargetAmount.Should().Be(25m);
        milestones[3].PercentComplete.Should().Be(100);
        milestones[3].TargetAmount.Should().Be(100m);
    }

    [Fact]
    public async Task CreateGoal_InvalidTargetAmount_ThrowsException()
    {
        // Arrange
        var child = await CreateChild();
        var dto = new CreateSavingsGoalDto(child.Id, "Goal", "Test", -50m);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _savingsGoalService.CreateGoalAsync(dto, _parentUserId));
    }

    [Fact]
    public async Task CreateGoal_InvalidAutoTransferPercent_ThrowsException()
    {
        // Arrange
        var child = await CreateChild();
        var dto = new CreateSavingsGoalDto(
            child.Id, "Goal", "Test", 100m, AutoTransferPercent: 150);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _savingsGoalService.CreateGoalAsync(dto, _parentUserId));
    }

    [Fact]
    public async Task CreateGoal_NonExistentChild_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateSavingsGoalDto(Guid.NewGuid(), "Goal", "Test", 100m);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _savingsGoalService.CreateGoalAsync(dto, _parentUserId));
    }

    // Deposit Tests (5)
    [Fact]
    public async Task DepositToGoal_ValidAmount_IncreasesGoalBalance()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var goal = await CreateGoal(child.Id, targetAmount: 100m);

        // Act
        var transaction = await _savingsGoalService.DepositToGoalAsync(
            goal.Id, 25m, "Saving for goal", _parentUserId);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(25m);
        transaction.BalanceAfter.Should().Be(25m);
        transaction.Type.Should().Be(SavingsTransactionType.Deposit);

        var updatedGoal = await _savingsGoalService.GetGoalAsync(goal.Id);
        updatedGoal.CurrentAmount.Should().Be(25m);

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(75m); // 100 - 25
        updatedChild.TotalGoalSavings.Should().Be(25m);
    }

    [Fact]
    public async Task DepositToGoal_InsufficientBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 10m);
        var goal = await CreateGoal(child.Id, targetAmount: 100m);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _savingsGoalService.DepositToGoalAsync(goal.Id, 50m, "Test", _parentUserId));
    }

    [Fact]
    public async Task DepositToGoal_NegativeAmount_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var goal = await CreateGoal(child.Id, targetAmount: 100m);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _savingsGoalService.DepositToGoalAsync(goal.Id, -10m, "Test", _parentUserId));
    }

    [Fact]
    public async Task DepositToGoal_ReachesMilestone_MarksMilestoneAchieved()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var goal = await CreateGoal(child.Id, targetAmount: 100m);

        // Act - Deposit 26 to reach 25% milestone
        await _savingsGoalService.DepositToGoalAsync(goal.Id, 26m, "Test", _parentUserId);

        // Assert
        var milestones = await _savingsGoalService.GetMilestonesAsync(goal.Id);
        var milestone25 = milestones.First(m => m.PercentComplete == 25);
        milestone25.IsAchieved.Should().BeTrue();
        milestone25.AchievedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DepositToGoal_ReachesTargetAmount_CompletesGoal()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var goal = await CreateGoal(child.Id, targetAmount: 50m);

        // Act
        await _savingsGoalService.DepositToGoalAsync(goal.Id, 50m, "Complete goal", _parentUserId);

        // Assert
        var updatedGoal = await _savingsGoalService.GetGoalAsync(goal.Id);
        updatedGoal.CompletedAt.Should().NotBeNull();
        updatedGoal.IsActive.Should().BeFalse();

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CompletedGoalsCount.Should().Be(1);
    }

    // Withdrawal Tests (3)
    [Fact]
    public async Task WithdrawFromGoal_ValidAmount_DecreasesGoalBalance()
    {
        // Arrange
        var child = await CreateChild(balance: 50m);
        var goal = await CreateGoalWithBalance(child.Id, targetAmount: 100m, currentAmount: 40m);

        // Act
        var transaction = await _savingsGoalService.WithdrawFromGoalAsync(
            goal.Id, 15m, "Need money back", _parentUserId);

        // Assert
        transaction.Amount.Should().Be(-15m); // Negative for withdrawal
        transaction.BalanceAfter.Should().Be(25m);
        transaction.Type.Should().Be(SavingsTransactionType.Withdrawal);

        var updatedGoal = await _savingsGoalService.GetGoalAsync(goal.Id);
        updatedGoal.CurrentAmount.Should().Be(25m);

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(65m); // 50 + 15
        updatedChild.TotalGoalSavings.Should().Be(25m);
    }

    [Fact]
    public async Task WithdrawFromGoal_ExceedsGoalBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 50m);
        var goal = await CreateGoalWithBalance(child.Id, targetAmount: 100m, currentAmount: 10m);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _savingsGoalService.WithdrawFromGoalAsync(goal.Id, 20m, "Test", _parentUserId));
    }

    [Fact]
    public async Task WithdrawFromGoal_NegativeAmount_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 50m);
        var goal = await CreateGoalWithBalance(child.Id, targetAmount: 100m, currentAmount: 30m);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _savingsGoalService.WithdrawFromGoalAsync(goal.Id, -5m, "Test", _parentUserId));
    }

    // Auto-Transfer Tests (5)
    [Fact]
    public async Task ProcessAutoTransfers_SingleGoal_TransfersCorrectAmount()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, weeklyAllowance: 20m);
        var goal = await CreateGoal(child.Id, targetAmount: 100m, autoTransferPercent: 25);
        var transaction = await CreateAllowanceTransaction(child.Id, 20m);

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, transaction.Id, 20m);

        // Assert
        var updatedGoal = await _savingsGoalService.GetGoalAsync(goal.Id);
        updatedGoal.CurrentAmount.Should().Be(5m); // 25% of 20

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(115m); // 100 + 20 - 5
    }

    [Fact]
    public async Task ProcessAutoTransfers_MultipleGoals_TransfersToEachByPriority()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, weeklyAllowance: 20m);
        var goal1 = await CreateGoal(child.Id, targetAmount: 50m, autoTransferPercent: 20, priority: 1);
        var goal2 = await CreateGoal(child.Id, targetAmount: 100m, autoTransferPercent: 30, priority: 2);
        var transaction = await CreateAllowanceTransaction(child.Id, 20m);

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, transaction.Id, 20m);

        // Assert
        var updatedGoal1 = await _savingsGoalService.GetGoalAsync(goal1.Id);
        updatedGoal1.CurrentAmount.Should().Be(4m); // 20% of 20

        var updatedGoal2 = await _savingsGoalService.GetGoalAsync(goal2.Id);
        updatedGoal2.CurrentAmount.Should().Be(6m); // 30% of 20

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(110m); // 100 + 20 - 4 - 6
    }

    [Fact]
    public async Task ProcessAutoTransfers_NoActiveGoals_DoesNothing()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, weeklyAllowance: 20m);
        var transaction = await CreateAllowanceTransaction(child.Id, 20m);

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, transaction.Id, 20m);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(120m); // No transfers
    }

    [Fact]
    public async Task ProcessAutoTransfers_InsufficientBalance_SkipsTransfer()
    {
        // Arrange
        var child = await CreateChild(balance: 2m, weeklyAllowance: 20m);
        var goal = await CreateGoal(child.Id, targetAmount: 100m, autoTransferPercent: 50);
        var transaction = await CreateAllowanceTransaction(child.Id, 20m);

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, transaction.Id, 20m);

        // Assert - Would need 10 (50% of 20) but only has 2 left after allowance
        var updatedGoal = await _savingsGoalService.GetGoalAsync(goal.Id);
        updatedGoal.CurrentAmount.Should().Be(0m); // Transfer skipped
    }

    [Fact]
    public async Task ProcessAutoTransfers_GoalCompleted_MarksComplete()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, weeklyAllowance: 20m);
        var goal = await CreateGoalWithBalance(
            child.Id, targetAmount: 100m, currentAmount: 95m, autoTransferPercent: 50);
        var transaction = await CreateAllowanceTransaction(child.Id, 20m);

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, transaction.Id, 20m);

        // Assert - 50% of 20 = 10, bringing total to 105 (over target)
        var updatedGoal = await _savingsGoalService.GetGoalAsync(goal.Id);
        updatedGoal.CurrentAmount.Should().Be(105m);
        updatedGoal.CompletedAt.Should().NotBeNull();
        updatedGoal.IsActive.Should().BeFalse();
    }

    // Milestone Tests (3)
    [Fact]
    public async Task CheckAndAwardMilestones_50PercentReached_Awards50Milestone()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var goal = await CreateGoal(child.Id, targetAmount: 100m);
        await _savingsGoalService.DepositToGoalAsync(goal.Id, 50m, "Halfway", _parentUserId);

        // Act
        await _savingsGoalService.CheckAndAwardMilestonesAsync(goal.Id);

        // Assert
        var milestones = await _savingsGoalService.GetMilestonesAsync(goal.Id);
        milestones.Where(m => m.PercentComplete <= 50).Should().AllSatisfy(m =>
        {
            m.IsAchieved.Should().BeTrue();
            m.AchievedAt.Should().NotBeNull();
        });
        milestones.Where(m => m.PercentComplete > 50).Should().AllSatisfy(m =>
        {
            m.IsAchieved.Should().BeFalse();
        });
    }

    [Fact]
    public async Task CheckAndAwardMilestones_AlreadyAwarded_DoesNotDuplicate()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var goal = await CreateGoal(child.Id, targetAmount: 100m);
        await _savingsGoalService.DepositToGoalAsync(goal.Id, 30m, "Test", _parentUserId);

        // Act - Check twice
        await _savingsGoalService.CheckAndAwardMilestonesAsync(goal.Id);
        var firstCheck = await _savingsGoalService.GetMilestonesAsync(goal.Id);
        var firstAchievedAt = firstCheck.First(m => m.PercentComplete == 25).AchievedAt;

        await _savingsGoalService.CheckAndAwardMilestonesAsync(goal.Id);
        var secondCheck = await _savingsGoalService.GetMilestonesAsync(goal.Id);
        var secondAchievedAt = secondCheck.First(m => m.PercentComplete == 25).AchievedAt;

        // Assert
        firstAchievedAt.Should().Be(secondAchievedAt);
    }

    [Fact]
    public async Task GetMilestones_ReturnsOrderedByPercent()
    {
        // Arrange
        var child = await CreateChild();
        var goal = await CreateGoal(child.Id, targetAmount: 100m);

        // Act
        var milestones = await _savingsGoalService.GetMilestonesAsync(goal.Id);

        // Assert
        milestones.Should().HaveCount(4);
        milestones[0].PercentComplete.Should().Be(25);
        milestones[1].PercentComplete.Should().Be(50);
        milestones[2].PercentComplete.Should().Be(75);
        milestones[3].PercentComplete.Should().Be(100);
    }

    // Progress & Statistics Tests (4)
    [Fact]
    public async Task GetProgress_ReturnsCorrectCalculations()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, weeklyAllowance: 10m);
        var goal = await CreateGoalWithBalance(
            child.Id, targetAmount: 100m, currentAmount: 40m, autoTransferPercent: 50);

        // Act
        var progress = await _savingsGoalService.GetProgressAsync(goal.Id);

        // Assert
        progress.CurrentAmount.Should().Be(40m);
        progress.TargetAmount.Should().Be(100m);
        progress.PercentComplete.Should().Be(40m);
        progress.AmountRemaining.Should().Be(60m);
        progress.WeeksToCompletion.Should().Be(12); // 60 / 5 (50% of 10) = 12 weeks
    }

    [Fact]
    public async Task CalculateWeeksToGoal_ReturnsCorrectEstimate()
    {
        // Act
        var weeks = _savingsGoalService.CalculateWeeksToGoal(
            currentAmount: 25m,
            targetAmount: 100m,
            weeklyAllowance: 15m,
            autoTransferPercent: 20);

        // Assert - Need 75 more, get 3/week (20% of 15), so 25 weeks
        weeks.Should().Be(25);
    }

    [Fact]
    public async Task CalculateWeeksToGoal_NoAutoTransfer_ReturnsMaxValue()
    {
        // Act
        var weeks = _savingsGoalService.CalculateWeeksToGoal(
            currentAmount: 10m,
            targetAmount: 100m,
            weeklyAllowance: 10m,
            autoTransferPercent: 0);

        // Assert
        weeks.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task GetStatistics_CalculatesCorrectly()
    {
        // Arrange
        var child = await CreateChild();
        var goal1 = await CreateGoalWithBalance(child.Id, 100m, 50m);
        var goal2 = await CreateGoalWithBalance(child.Id, 200m, 100m);
        var goal3 = await CreateGoal(child.Id, 50m);
        await _savingsGoalService.CompleteGoalAsync(goal3.Id, _parentUserId);

        // Act
        var stats = await _savingsGoalService.GetStatisticsAsync(child.Id);

        // Assert
        stats.TotalGoals.Should().Be(3);
        stats.ActiveGoals.Should().Be(2);
        stats.CompletedGoals.Should().Be(1);
        stats.TotalSaved.Should().Be(150m); // 50 + 100 + 0
        stats.TotalTargetAmount.Should().Be(300m); // 100 + 200 (active only)
    }

    // Goal Management Tests (5)
    [Fact]
    public async Task UpdateGoal_ValidData_UpdatesSuccessfully()
    {
        // Arrange
        var child = await CreateChild();
        var goal = await CreateGoal(child.Id, targetAmount: 100m);
        var updateDto = new UpdateSavingsGoalDto(
            Name: "Updated Name",
            TargetAmount: 150m,
            AutoTransferPercent: 30);

        // Act
        var updated = await _savingsGoalService.UpdateGoalAsync(goal.Id, updateDto, _parentUserId);

        // Assert
        updated.Name.Should().Be("Updated Name");
        updated.TargetAmount.Should().Be(150m);
        updated.AutoTransferPercent.Should().Be(30);
    }

    [Fact]
    public async Task GetActiveGoals_ReturnsOnlyActiveGoals()
    {
        // Arrange
        var child = await CreateChild();
        var goal1 = await CreateGoal(child.Id, targetAmount: 100m);
        var goal2 = await CreateGoal(child.Id, targetAmount: 50m);
        await _savingsGoalService.CompleteGoalAsync(goal2.Id, _parentUserId);

        // Act
        var activeGoals = await _savingsGoalService.GetActiveGoalsAsync(child.Id);

        // Assert
        activeGoals.Should().HaveCount(1);
        activeGoals[0].Id.Should().Be(goal1.Id);
    }

    [Fact]
    public async Task GetCompletedGoals_ReturnsOnlyCompletedGoals()
    {
        // Arrange
        var child = await CreateChild();
        var goal1 = await CreateGoal(child.Id, targetAmount: 100m);
        var goal2 = await CreateGoal(child.Id, targetAmount: 50m);
        await _savingsGoalService.CompleteGoalAsync(goal2.Id, _parentUserId);

        // Act
        var completedGoals = await _savingsGoalService.GetCompletedGoalsAsync(child.Id);

        // Assert
        completedGoals.Should().HaveCount(1);
        completedGoals[0].Id.Should().Be(goal2.Id);
    }

    [Fact]
    public async Task CompleteGoal_AlreadyCompleted_ThrowsException()
    {
        // Arrange
        var child = await CreateChild();
        var goal = await CreateGoal(child.Id, targetAmount: 50m);
        await _savingsGoalService.CompleteGoalAsync(goal.Id, _parentUserId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _savingsGoalService.CompleteGoalAsync(goal.Id, _parentUserId));
    }

    [Fact]
    public async Task ReactivateGoal_CompletedGoal_MakesActive()
    {
        // Arrange
        var child = await CreateChild();
        var goal = await CreateGoal(child.Id, targetAmount: 50m);
        await _savingsGoalService.CompleteGoalAsync(goal.Id, _parentUserId);

        // Act
        var reactivated = await _savingsGoalService.ReactivateGoalAsync(goal.Id, _parentUserId);

        // Assert
        reactivated.IsActive.Should().BeTrue();
        reactivated.CompletedAt.Should().BeNull();
    }
}
```

---

## Phase 3: API Controllers

### 3.1 SavingsGoalsController

```csharp
namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/savings-goals")]
[Authorize]
public class SavingsGoalsController : ControllerBase
{
    private readonly ISavingsGoalService _savingsGoalService;
    private readonly ICurrentUserService _currentUserService;

    public SavingsGoalsController(
        ISavingsGoalService savingsGoalService,
        ICurrentUserService currentUserService)
    {
        _savingsGoalService = savingsGoalService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Create a new savings goal
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WishListItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WishListItem>> CreateGoal(CreateSavingsGoalDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var goal = await _savingsGoalService.CreateGoalAsync(dto, userId);
        return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, goal);
    }

    /// <summary>
    /// Get goal by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WishListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishListItem>> GetGoal(Guid id)
    {
        var goal = await _savingsGoalService.GetGoalAsync(id);
        return Ok(goal);
    }

    /// <summary>
    /// Get all active goals for a child
    /// </summary>
    [HttpGet("child/{childId}/active")]
    [ProducesResponseType(typeof(List<WishListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WishListItem>>> GetActiveGoals(Guid childId)
    {
        var goals = await _savingsGoalService.GetActiveGoalsAsync(childId);
        return Ok(goals);
    }

    /// <summary>
    /// Get all completed goals for a child
    /// </summary>
    [HttpGet("child/{childId}/completed")]
    [ProducesResponseType(typeof(List<WishListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WishListItem>>> GetCompletedGoals(Guid childId)
    {
        var goals = await _savingsGoalService.GetCompletedGoalsAsync(childId);
        return Ok(goals);
    }

    /// <summary>
    /// Update goal details
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(WishListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishListItem>> UpdateGoal(Guid id, UpdateSavingsGoalDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var goal = await _savingsGoalService.UpdateGoalAsync(id, dto, userId);
        return Ok(goal);
    }

    /// <summary>
    /// Delete a goal
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteGoal(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        await _savingsGoalService.DeleteGoalAsync(id, userId);
        return NoContent();
    }

    /// <summary>
    /// Deposit money to a goal
    /// </summary>
    [HttpPost("{id}/deposit")]
    [ProducesResponseType(typeof(SavingsGoalTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavingsGoalTransaction>> DepositToGoal(
        Guid id,
        [FromBody] DepositRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var transaction = await _savingsGoalService.DepositToGoalAsync(
            id, request.Amount, request.Description, userId);
        return Ok(transaction);
    }

    /// <summary>
    /// Withdraw money from a goal
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [ProducesResponseType(typeof(SavingsGoalTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavingsGoalTransaction>> WithdrawFromGoal(
        Guid id,
        [FromBody] WithdrawRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var transaction = await _savingsGoalService.WithdrawFromGoalAsync(
            id, request.Amount, request.Description, userId);
        return Ok(transaction);
    }

    /// <summary>
    /// Get goal progress and statistics
    /// </summary>
    [HttpGet("{id}/progress")]
    [ProducesResponseType(typeof(SavingsGoalProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavingsGoalProgressDto>> GetProgress(Guid id)
    {
        var progress = await _savingsGoalService.GetProgressAsync(id);
        return Ok(progress);
    }

    /// <summary>
    /// Get milestones for a goal
    /// </summary>
    [HttpGet("{id}/milestones")]
    [ProducesResponseType(typeof(List<SavingsGoalMilestone>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SavingsGoalMilestone>>> GetMilestones(Guid id)
    {
        var milestones = await _savingsGoalService.GetMilestonesAsync(id);
        return Ok(milestones);
    }

    /// <summary>
    /// Get transaction history for a goal
    /// </summary>
    [HttpGet("{id}/transactions")]
    [ProducesResponseType(typeof(List<SavingsGoalTransaction>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SavingsGoalTransaction>>> GetTransactions(Guid id)
    {
        var transactions = await _savingsGoalService.GetTransactionHistoryAsync(id);
        return Ok(transactions);
    }

    /// <summary>
    /// Get savings statistics for a child
    /// </summary>
    [HttpGet("child/{childId}/statistics")]
    [ProducesResponseType(typeof(SavingsGoalStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<SavingsGoalStatistics>> GetStatistics(Guid childId)
    {
        var stats = await _savingsGoalService.GetStatisticsAsync(childId);
        return Ok(stats);
    }

    /// <summary>
    /// Mark goal as complete
    /// </summary>
    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(WishListItem), StatusCodes.Status200OK)]
    public async Task<ActionResult<WishListItem>> CompleteGoal(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var goal = await _savingsGoalService.CompleteGoalAsync(id, userId);
        return Ok(goal);
    }

    /// <summary>
    /// Reactivate a completed goal
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(WishListItem), StatusCodes.Status200OK)]
    public async Task<ActionResult<WishListItem>> ReactivateGoal(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var goal = await _savingsGoalService.ReactivateGoalAsync(id, userId);
        return Ok(goal);
    }
}

public record DepositRequest(decimal Amount, string Description);
public record WithdrawRequest(decimal Amount, string Description);
```

---

## Phase 4: Blazor UI Components

### 4.1 SavingsGoalCard Component

```razor
@inject ISavingsGoalService SavingsGoalService
@inject NavigationManager Navigation

<div class="card savings-goal-card @GetStatusClass()">
    <div class="card-body">
        @if (!string.IsNullOrEmpty(Goal.ImageUrl))
        {
            <img src="@Goal.ImageUrl" class="goal-image mb-3" alt="@Goal.Name" />
        }

        <div class="d-flex justify-content-between align-items-start mb-2">
            <h5 class="card-title mb-0">@Goal.Name</h5>
            <div class="goal-priority">
                <span class="badge bg-info">Priority @Goal.Priority</span>
            </div>
        </div>

        <p class="text-muted small">@Goal.Description</p>

        <div class="goal-amounts mb-3">
            <div class="d-flex justify-content-between">
                <strong>@Goal.CurrentAmount.ToString("C")</strong>
                <span class="text-muted">of @Goal.TargetAmount.ToString("C")</span>
            </div>
        </div>

        <!-- Progress Bar -->
        <div class="progress mb-2" style="height: 30px;">
            <div class="progress-bar @GetProgressBarClass()"
                 role="progressbar"
                 style="width: @GetPercentComplete()%"
                 aria-valuenow="@GetPercentComplete()"
                 aria-valuemin="0"
                 aria-valuemax="100">
                @GetPercentComplete().ToString("F0")%
            </div>
        </div>

        <!-- Milestones -->
        <div class="milestones mb-3">
            <div class="d-flex justify-content-between">
                @foreach (var milestone in GetMilestonePercentages())
                {
                    <div class="milestone @(IsMilestoneReached(milestone) ? "achieved" : "")">
                        <span class="milestone-icon">
                            @(IsMilestoneReached(milestone) ? "✓" : milestone + "%")
                        </span>
                    </div>
                }
            </div>
        </div>

        <!-- Auto-transfer badge -->
        @if (Goal.AutoTransferPercent > 0)
        {
            <div class="alert alert-info py-1 px-2 small mb-2">
                🔄 Auto-saving @Goal.AutoTransferPercent% of allowance
            </div>
        }

        <!-- Target date -->
        @if (Goal.TargetDate.HasValue)
        {
            <div class="text-muted small mb-2">
                <span class="oi oi-calendar"></span>
                Target: @Goal.TargetDate.Value.ToString("MMM dd, yyyy")
                @if (Progress != null && Progress.EstimatedCompletionDate.HasValue)
                {
                    <br />
                    <span class="oi oi-clock"></span>
                    Est. completion: @Progress.EstimatedCompletionDate.Value.ToString("MMM dd, yyyy")
                    (@Progress.WeeksToCompletion weeks)
                }
            </div>
        }

        <!-- Actions -->
        <div class="btn-group w-100">
            <button class="btn btn-sm btn-success" @onclick="OnDepositClick">
                <span class="oi oi-plus"></span> Add Money
            </button>
            @if (Goal.CurrentAmount > 0)
            {
                <button class="btn btn-sm btn-warning" @onclick="OnWithdrawClick">
                    <span class="oi oi-minus"></span> Withdraw
                </button>
            }
            <button class="btn btn-sm btn-info" @onclick="OnDetailsClick">
                <span class="oi oi-info"></span> Details
            </button>
        </div>

        @if (!string.IsNullOrEmpty(Goal.ProductUrl))
        {
            <a href="@Goal.ProductUrl" target="_blank" class="btn btn-sm btn-outline-primary w-100 mt-2">
                <span class="oi oi-external-link"></span> View Product
            </a>
        }
    </div>
</div>

@code {
    [Parameter] public WishListItem Goal { get; set; } = null!;
    [Parameter] public EventCallback OnGoalUpdated { get; set; }

    private SavingsGoalProgressDto? Progress;

    protected override async Task OnInitializedAsync()
    {
        Progress = await SavingsGoalService.GetProgressAsync(Goal.Id);
    }

    private decimal GetPercentComplete()
    {
        return Goal.TargetAmount > 0
            ? Math.Min((Goal.CurrentAmount / Goal.TargetAmount) * 100, 100)
            : 0;
    }

    private string GetProgressBarClass()
    {
        var percent = GetPercentComplete();
        if (percent >= 100) return "bg-success";
        if (percent >= 75) return "bg-info";
        if (percent >= 50) return "bg-warning";
        return "bg-danger";
    }

    private string GetStatusClass()
    {
        if (Goal.CompletedAt != null) return "border-success";
        return "";
    }

    private int[] GetMilestonePercentages() => new[] { 25, 50, 75, 100 };

    private bool IsMilestoneReached(int percent)
    {
        return GetPercentComplete() >= percent;
    }

    private async Task OnDepositClick()
    {
        Navigation.NavigateTo($"/savings-goals/{Goal.Id}/deposit");
    }

    private async Task OnWithdrawClick()
    {
        Navigation.NavigateTo($"/savings-goals/{Goal.Id}/withdraw");
    }

    private void OnDetailsClick()
    {
        Navigation.NavigateTo($"/savings-goals/{Goal.Id}");
    }
}
```

### 4.2 SavingsGoalsList Component

```razor
@page "/savings-goals/{ChildId:guid}"
@inject ISavingsGoalService SavingsGoalService
@inject NavigationManager Navigation

<div class="savings-goals-list">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h3>Savings Goals</h3>
        <button class="btn btn-primary" @onclick="OnCreateGoal">
            <span class="oi oi-plus"></span> New Goal
        </button>
    </div>

    @if (Loading)
    {
        <div class="text-center">
            <div class="spinner-border" role="status"></div>
        </div>
    }
    else
    {
        <!-- Statistics Summary -->
        @if (Statistics != null)
        {
            <div class="row mb-4">
                <div class="col-md-3">
                    <div class="stat-card">
                        <div class="stat-value">@Statistics.ActiveGoals</div>
                        <div class="stat-label">Active Goals</div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="stat-card">
                        <div class="stat-value">@Statistics.TotalSaved.ToString("C")</div>
                        <div class="stat-label">Total Saved</div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="stat-card">
                        <div class="stat-value">@Statistics.CompletedGoals</div>
                        <div class="stat-label">Completed</div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="stat-card">
                        <div class="stat-value">@Statistics.OverallCompletionRate.ToString("F0")%</div>
                        <div class="stat-label">Overall Progress</div>
                    </div>
                </div>
            </div>
        }

        <!-- Active Goals -->
        @if (ActiveGoals.Any())
        {
            <h4 class="mb-3">Active Goals</h4>
            <div class="row">
                @foreach (var goal in ActiveGoals)
                {
                    <div class="col-md-6 col-lg-4 mb-3">
                        <SavingsGoalCard Goal="@goal" OnGoalUpdated="@RefreshGoals" />
                    </div>
                }
            </div>
        }
        else
        {
            <div class="alert alert-info">
                <h5>No active savings goals</h5>
                <p>Create your first goal to start saving!</p>
                <button class="btn btn-primary" @onclick="OnCreateGoal">
                    Create Goal
                </button>
            </div>
        }

        <!-- Completed Goals -->
        @if (CompletedGoals.Any())
        {
            <h4 class="mt-4 mb-3">Completed Goals 🎉</h4>
            <div class="row">
                @foreach (var goal in CompletedGoals.Take(6))
                {
                    <div class="col-md-6 col-lg-4 mb-3">
                        <SavingsGoalCard Goal="@goal" OnGoalUpdated="@RefreshGoals" />
                    </div>
                }
            </div>
        }
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }

    private List<WishListItem> ActiveGoals = new();
    private List<WishListItem> CompletedGoals = new();
    private SavingsGoalStatistics? Statistics;
    private bool Loading = true;

    protected override async Task OnInitializedAsync()
    {
        await RefreshGoals();
    }

    private async Task RefreshGoals()
    {
        Loading = true;
        ActiveGoals = await SavingsGoalService.GetActiveGoalsAsync(ChildId);
        CompletedGoals = await SavingsGoalService.GetCompletedGoalsAsync(ChildId);
        Statistics = await SavingsGoalService.GetStatisticsAsync(ChildId);
        Loading = false;
        StateHasChanged();
    }

    private void OnCreateGoal()
    {
        Navigation.NavigateTo($"/savings-goals/create/{ChildId}");
    }
}
```

### 4.3 CreateSavingsGoalForm Component

```razor
@page "/savings-goals/create/{ChildId:guid}"
@inject ISavingsGoalService SavingsGoalService
@inject NavigationManager Navigation

<h3>Create Savings Goal</h3>

<EditForm Model="@formModel" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label for="name" class="form-label">Goal Name</label>
        <InputText id="name" @bind-Value="formModel.Name" class="form-control" />
    </div>

    <div class="mb-3">
        <label for="description" class="form-label">Description</label>
        <InputTextArea id="description" @bind-Value="formModel.Description" class="form-control" rows="3" />
    </div>

    <div class="row">
        <div class="col-md-6 mb-3">
            <label for="targetAmount" class="form-label">Target Amount</label>
            <div class="input-group">
                <span class="input-group-text">$</span>
                <InputNumber id="targetAmount" @bind-Value="formModel.TargetAmount" class="form-control" />
            </div>
        </div>

        <div class="col-md-6 mb-3">
            <label for="autoTransferPercent" class="form-label">
                Auto-Transfer % of Allowance
            </label>
            <div class="input-group">
                <InputNumber id="autoTransferPercent"
                            @bind-Value="formModel.AutoTransferPercent"
                            class="form-control"
                            min="0"
                            max="100" />
                <span class="input-group-text">%</span>
            </div>
            <small class="text-muted">
                Automatically save this percentage when allowance is received
            </small>
        </div>
    </div>

    <div class="row">
        <div class="col-md-6 mb-3">
            <label for="targetDate" class="form-label">Target Date (optional)</label>
            <InputDate id="targetDate" @bind-Value="formModel.TargetDate" class="form-control" />
        </div>

        <div class="col-md-6 mb-3">
            <label for="priority" class="form-label">Priority</label>
            <InputNumber id="priority" @bind-Value="formModel.Priority" class="form-control" min="1" max="10" />
            <small class="text-muted">Lower number = higher priority</small>
        </div>
    </div>

    <div class="mb-3">
        <label for="imageUrl" class="form-label">Image URL (optional)</label>
        <InputText id="imageUrl" @bind-Value="formModel.ImageUrl" class="form-control" />
    </div>

    <div class="mb-3">
        <label for="productUrl" class="form-label">Product URL (optional)</label>
        <InputText id="productUrl" @bind-Value="formModel.ProductUrl" class="form-control" />
    </div>

    <div class="mb-3">
        <label for="notes" class="form-label">Notes</label>
        <InputTextArea id="notes" @bind-Value="formModel.Notes" class="form-control" rows="2" />
    </div>

    <div class="d-flex gap-2">
        <button type="submit" class="btn btn-primary" disabled="@submitting">
            @if (submitting)
            {
                <span class="spinner-border spinner-border-sm me-2"></span>
            }
            Create Goal
        </button>
        <button type="button" class="btn btn-secondary" @onclick="Cancel">
            Cancel
        </button>
    </div>
</EditForm>

@code {
    [Parameter] public Guid ChildId { get; set; }

    private CreateSavingsGoalFormModel formModel = new();
    private bool submitting = false;

    protected override void OnInitialized()
    {
        formModel.ChildId = ChildId;
    }

    private async Task HandleSubmit()
    {
        submitting = true;

        try
        {
            var dto = new CreateSavingsGoalDto(
                formModel.ChildId,
                formModel.Name!,
                formModel.Description!,
                formModel.TargetAmount,
                formModel.TargetDate,
                formModel.AutoTransferPercent,
                formModel.Priority,
                formModel.ImageUrl,
                formModel.ProductUrl,
                formModel.Notes);

            var goal = await SavingsGoalService.CreateGoalAsync(dto, Guid.Empty); // TODO: Get current user
            Navigation.NavigateTo($"/savings-goals/{ChildId}");
        }
        finally
        {
            submitting = false;
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo($"/savings-goals/{ChildId}");
    }

    private class CreateSavingsGoalFormModel
    {
        public Guid ChildId { get; set; }

        [Required, StringLength(100)]
        public string? Name { get; set; }

        [Required, StringLength(500)]
        public string? Description { get; set; }

        [Required, Range(0.01, 100000)]
        public decimal TargetAmount { get; set; }

        [Range(0, 100)]
        public int AutoTransferPercent { get; set; } = 0;

        public DateTime? TargetDate { get; set; }

        [Range(1, 10)]
        public int Priority { get; set; } = 1;

        public string? ImageUrl { get; set; }
        public string? ProductUrl { get; set; }
        public string? Notes { get; set; }
    }
}
```

---

## Success Metrics

- ✅ All 30 tests passing
- ✅ Goals created with auto-transfer configuration
- ✅ Milestones automatically created and tracked
- ✅ Auto-transfers processed on allowance payments
- ✅ Milestone achievements celebrated with notifications
- ✅ Goal completion tracked and celebrated
- ✅ Progress visualization accurate and real-time
- ✅ Statistics calculated correctly

---

## Future Enhancements

1. **Family Matching**: Parents match X% of child's savings
2. **Goal Templates**: Pre-made goal templates (bike, game console, etc.)
3. **Savings Challenges**: Time-limited challenges with bonus rewards
4. **Goal Sharing**: Share goals with family/friends for gift giving
5. **Photo Proof**: Upload photos when goal is reached
6. **Goal Categories**: Organize goals by category (toys, education, charity)
7. **Stretch Goals**: Bonus milestones beyond 100%
8. **Goal Analytics**: Charts showing savings velocity and trends

---

**Total Implementation Time**: 3-4 weeks following TDD methodology
