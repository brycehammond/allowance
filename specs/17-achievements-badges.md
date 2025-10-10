# Achievements & Badges Gamification Specification

## Overview

This specification introduces a comprehensive achievement and badge system that gamifies financial learning. Children unlock 30+ achievements for various financial behaviors - from their first transaction to reaching savings goals. This motivates positive financial habits through recognition and rewards.

## Goals

1. **Motivate Learning**: Reward financial milestones and positive behaviors
2. **30+ Achievements**: Diverse badges across different categories
3. **Rarity Levels**: Common, Rare, Epic, Legendary achievements
4. **Event-Driven**: Automatic unlocking based on system events
5. **Badge Gallery**: Visual display of earned and locked badges
6. **Leaderboard**: Optional family competition for achievements
7. **TDD Approach**: 50 comprehensive tests

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Entity Framework Core
- **Database**: PostgreSQL with JSON column support for criteria
- **Testing**: xUnit, FluentAssertions, Moq
- **Events**: Event-driven architecture for achievement triggers
- **UI**: Blazor Server with animated badge unlocks

---

## Phase 1: Database Schema

### 1.1 Achievement Model (Predefined Achievements)

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Predefined achievement definitions (seeded data)
/// </summary>
public class Achievement
{
    public Guid Id { get; set; }

    /// <summary>
    /// Achievement unique code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Achievement title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Achievement description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Icon/emoji for achievement
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Achievement category
    /// </summary>
    public AchievementType Type { get; set; }

    /// <summary>
    /// Achievement rarity
    /// </summary>
    public RarityLevel Rarity { get; set; }

    /// <summary>
    /// JSON criteria for unlocking
    /// </summary>
    public string CriteriaJson { get; set; } = string.Empty;

    /// <summary>
    /// Reward amount (if applicable)
    /// </summary>
    public decimal? RewardAmount { get; set; }

    /// <summary>
    /// Points earned
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Is achievement active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<ChildAchievement> ChildAchievements { get; set; } = new List<ChildAchievement>();
}

public enum AchievementType
{
    FirstTime = 1,      // First transaction, first savings, etc.
    Milestone = 2,      // Reach $X balance, complete X goals
    Streak = 3,         // Consecutive weeks of saving
    Savings = 4,        // Savings-related achievements
    Spending = 5,       // Smart spending achievements
    Chores = 6,         // Chore completion achievements
    Goals = 7,          // Goal achievement badges
    Collection = 8,     // Complete sets of achievements
    Special = 9         // Seasonal, event-based
}

public enum RarityLevel
{
    Common = 1,         // Easy to earn
    Rare = 2,           // Moderate difficulty
    Epic = 3,           // Hard to earn
    Legendary = 4       // Very rare, prestigious
}
```

### 1.2 ChildAchievement Model (Earned Badges)

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Tracks which achievements children have earned
/// </summary>
public class ChildAchievement
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid AchievementId { get; set; }
    public virtual Achievement Achievement { get; set; } = null!;

    /// <summary>
    /// When achievement was unlocked
    /// </summary>
    public DateTime UnlockedAt { get; set; }

    /// <summary>
    /// Progress value when unlocked (for tracking)
    /// </summary>
    public decimal? ProgressValue { get; set; }

    /// <summary>
    /// Was notification shown?
    /// </summary>
    public bool NotificationShown { get; set; } = false;

    /// <summary>
    /// Was reward claimed?
    /// </summary>
    public bool RewardClaimed { get; set; } = false;

    /// <summary>
    /// Transaction created for reward (if applicable)
    /// </summary>
    public Guid? RewardTransactionId { get; set; }
}
```

### 1.3 Update Child Model

```csharp
namespace AllowanceTracker.Models;

public class Child
{
    // ... existing properties ...

    /// <summary>
    /// Total achievement points earned
    /// </summary>
    public int TotalAchievementPoints { get; set; } = 0;

    /// <summary>
    /// Number of achievements unlocked
    /// </summary>
    public int AchievementsUnlocked { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<ChildAchievement> Achievements { get; set; } = new List<ChildAchievement>();
}
```

### 1.4 Database Migration

```bash
dotnet ef migrations add AddAchievementSystem
```

```csharp
public partial class AddAchievementSystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Achievements table
        migrationBuilder.CreateTable(
            name: "Achievements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "varchar(100)", nullable: false),
                Title = table.Column<string>(type: "varchar(200)", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Icon = table.Column<string>(type: "varchar(50)", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Rarity = table.Column<int>(type: "integer", nullable: false),
                CriteriaJson = table.Column<string>(type: "jsonb", nullable: false),
                RewardAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                Points = table.Column<int>(type: "integer", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Achievements", x => x.Id);
            });

        // Create ChildAchievements table
        migrationBuilder.CreateTable(
            name: "ChildAchievements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProgressValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                NotificationShown = table.Column<bool>(type: "boolean", nullable: false),
                RewardClaimed = table.Column<bool>(type: "boolean", nullable: false),
                RewardTransactionId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChildAchievements", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChildAchievements_Children_ChildId",
                    column: x => x.ChildId,
                    principalTable: "Children",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ChildAchievements_Achievements_AchievementId",
                    column: x => x.AchievementId,
                    principalTable: "Achievements",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_Achievements_Code",
            table: "Achievements",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Achievements_Type_Rarity",
            table: "Achievements",
            columns: new[] { "Type", "Rarity" });

        migrationBuilder.CreateIndex(
            name: "IX_ChildAchievements_ChildId_AchievementId",
            table: "ChildAchievements",
            columns: new[] { "ChildId", "AchievementId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChildAchievements_UnlockedAt",
            table: "ChildAchievements",
            column: "UnlockedAt");

        // Add columns to Children table
        migrationBuilder.AddColumn<int>(
            name: "TotalAchievementPoints",
            table: "Children",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "AchievementsUnlocked",
            table: "Children",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
```

### 1.5 Achievement Seed Data

```csharp
public static class AchievementSeeder
{
    public static List<Achievement> GetAchievements()
    {
        return new List<Achievement>
        {
            // First Time Achievements (Common)
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "FIRST_TRANSACTION",
                Title = "First Step",
                Description = "Complete your first transaction",
                Icon = "🎯",
                Type = AchievementType.FirstTime,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"transactionCount\",\"value\":1}",
                RewardAmount = 1.00m,
                Points = 10,
                SortOrder = 1
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "FIRST_SAVINGS",
                Title = "Piggy Bank",
                Description = "Save money for the first time",
                Icon = "🐷",
                Type = AchievementType.FirstTime,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"savingsGoalDeposit\",\"value\":1}",
                RewardAmount = 2.00m,
                Points = 15,
                SortOrder = 2
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "FIRST_GOAL",
                Title = "Goal Setter",
                Description = "Create your first savings goal",
                Icon = "🎯",
                Type = AchievementType.FirstTime,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"goalsCreated\",\"value\":1}",
                Points = 10,
                SortOrder = 3
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "FIRST_CHORE",
                Title = "Hard Worker",
                Description = "Complete your first chore",
                Icon = "💪",
                Type = AchievementType.FirstTime,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"choresCompleted\",\"value\":1}",
                RewardAmount = 1.50m,
                Points = 15,
                SortOrder = 4
            },

            // Milestone Achievements (Common to Rare)
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "BALANCE_10",
                Title = "Ten Dollar Club",
                Description = "Reach a balance of $10",
                Icon = "💵",
                Type = AchievementType.Milestone,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"balance\",\"value\":10}",
                Points = 20,
                SortOrder = 10
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "BALANCE_50",
                Title = "Halfway to 100",
                Description = "Reach a balance of $50",
                Icon = "💰",
                Type = AchievementType.Milestone,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"balance\",\"value\":50}",
                RewardAmount = 2.50m,
                Points = 50,
                SortOrder = 11
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "BALANCE_100",
                Title = "Century",
                Description = "Reach a balance of $100",
                Icon = "💎",
                Type = AchievementType.Milestone,
                Rarity = RarityLevel.Rare,
                CriteriaJson = "{\"type\":\"balance\",\"value\":100}",
                RewardAmount = 5.00m,
                Points = 100,
                SortOrder = 12
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "BALANCE_250",
                Title = "Super Saver",
                Description = "Reach a balance of $250",
                Icon = "🏆",
                Type = AchievementType.Milestone,
                Rarity = RarityLevel.Epic,
                CriteriaJson = "{\"type\":\"balance\",\"value\":250}",
                RewardAmount = 10.00m,
                Points = 250,
                SortOrder = 13
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "BALANCE_500",
                Title = "Money Master",
                Description = "Reach a balance of $500",
                Icon = "👑",
                Type = AchievementType.Milestone,
                Rarity = RarityLevel.Legendary,
                CriteriaJson = "{\"type\":\"balance\",\"value\":500}",
                RewardAmount = 25.00m,
                Points = 500,
                SortOrder = 14
            },

            // Streak Achievements (Rare to Legendary)
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "STREAK_3",
                Title = "On Fire",
                Description = "Save money 3 weeks in a row",
                Icon = "🔥",
                Type = AchievementType.Streak,
                Rarity = RarityLevel.Rare,
                CriteriaJson = "{\"type\":\"savingsStreak\",\"value\":3}",
                RewardAmount = 3.00m,
                Points = 75,
                SortOrder = 20
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "STREAK_10",
                Title = "Unstoppable",
                Description = "Save money 10 weeks in a row",
                Icon = "⚡",
                Type = AchievementType.Streak,
                Rarity = RarityLevel.Epic,
                CriteriaJson = "{\"type\":\"savingsStreak\",\"value\":10}",
                RewardAmount = 10.00m,
                Points = 200,
                SortOrder = 21
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "STREAK_20",
                Title = "Legend",
                Description = "Save money 20 weeks in a row",
                Icon = "💫",
                Type = AchievementType.Streak,
                Rarity = RarityLevel.Legendary,
                CriteriaJson = "{\"type\":\"savingsStreak\",\"value\":20}",
                RewardAmount = 25.00m,
                Points = 500,
                SortOrder = 22
            },

            // Savings Goal Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "GOAL_COMPLETE_1",
                Title = "Goal Achieved",
                Description = "Complete your first savings goal",
                Icon = "🎉",
                Type = AchievementType.Goals,
                Rarity = RarityLevel.Rare,
                CriteriaJson = "{\"type\":\"goalsCompleted\",\"value\":1}",
                RewardAmount = 5.00m,
                Points = 100,
                SortOrder = 30
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "GOAL_COMPLETE_5",
                Title = "Goal Crusher",
                Description = "Complete 5 savings goals",
                Icon = "⭐",
                Type = AchievementType.Goals,
                Rarity = RarityLevel.Epic,
                CriteriaJson = "{\"type\":\"goalsCompleted\",\"value\":5}",
                RewardAmount = 15.00m,
                Points = 300,
                SortOrder = 31
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "GOAL_BIG_SAVER",
                Title = "Big Dreamer",
                Description = "Complete a goal worth $100+",
                Icon = "🌟",
                Type = AchievementType.Goals,
                Rarity = RarityLevel.Epic,
                CriteriaJson = "{\"type\":\"goalAmount\",\"value\":100}",
                RewardAmount = 10.00m,
                Points = 250,
                SortOrder = 32
            },

            // Chore Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "CHORES_10",
                Title = "Helper",
                Description = "Complete 10 chores",
                Icon = "🧹",
                Type = AchievementType.Chores,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"choresCompleted\",\"value\":10}",
                Points = 50,
                SortOrder = 40
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "CHORES_50",
                Title = "Super Helper",
                Description = "Complete 50 chores",
                Icon = "🏠",
                Type = AchievementType.Chores,
                Rarity = RarityLevel.Rare,
                CriteriaJson = "{\"type\":\"choresCompleted\",\"value\":50}",
                RewardAmount = 10.00m,
                Points = 150,
                SortOrder = 41
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "CHORES_100",
                Title = "Work Ethic Champion",
                Description = "Complete 100 chores",
                Icon = "🥇",
                Type = AchievementType.Chores,
                Rarity = RarityLevel.Epic,
                CriteriaJson = "{\"type\":\"choresCompleted\",\"value\":100}",
                RewardAmount = 25.00m,
                Points = 400,
                SortOrder = 42
            },

            // Smart Spending Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "BUDGET_KEEPER",
                Title = "Budget Keeper",
                Description = "Stay under budget for a full month",
                Icon = "📊",
                Type = AchievementType.Spending,
                Rarity = RarityLevel.Rare,
                CriteriaJson = "{\"type\":\"budgetCompliance\",\"value\":30}",
                RewardAmount = 5.00m,
                Points = 100,
                SortOrder = 50
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "WISE_SPENDER",
                Title = "Wise Spender",
                Description = "Make smart purchases with parent approval",
                Icon = "🧠",
                Type = AchievementType.Spending,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"approvedRequests\",\"value\":10}",
                Points = 50,
                SortOrder = 51
            },

            // Collection Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "COLLECTOR",
                Title = "Badge Collector",
                Description = "Earn 10 different achievements",
                Icon = "🎖️",
                Type = AchievementType.Collection,
                Rarity = RarityLevel.Rare,
                CriteriaJson = "{\"type\":\"achievementCount\",\"value\":10}",
                RewardAmount = 10.00m,
                Points = 200,
                SortOrder = 60
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "MASTER_COLLECTOR",
                Title = "Achievement Master",
                Description = "Earn 20 different achievements",
                Icon = "🏅",
                Type = AchievementType.Collection,
                Rarity = RarityLevel.Epic,
                CriteriaJson = "{\"type\":\"achievementCount\",\"value\":20}",
                RewardAmount = 25.00m,
                Points = 500,
                SortOrder = 61
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "COMPLETIONIST",
                Title = "Completionist",
                Description = "Earn ALL available achievements",
                Icon = "👑",
                Type = AchievementType.Collection,
                Rarity = RarityLevel.Legendary,
                CriteriaJson = "{\"type\":\"allAchievements\",\"value\":100}",
                RewardAmount = 50.00m,
                Points = 1000,
                SortOrder = 62
            },

            // Special/Seasonal Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "BIRTHDAY_SAVER",
                Title = "Birthday Saver",
                Description = "Save birthday money instead of spending it all",
                Icon = "🎂",
                Type = AchievementType.Special,
                Rarity = RarityLevel.Rare,
                CriteriaJson = "{\"type\":\"birthdayDeposit\",\"value\":1}",
                RewardAmount = 5.00m,
                Points = 100,
                SortOrder = 70
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "EARLY_BIRD",
                Title = "Early Bird",
                Description = "Create account and start saving immediately",
                Icon = "🐦",
                Type = AchievementType.Special,
                Rarity = RarityLevel.Common,
                CriteriaJson = "{\"type\":\"daysSinceJoin\",\"value\":1}",
                Points = 25,
                SortOrder = 71
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "ONE_YEAR",
                Title = "Veteran Saver",
                Description = "One year of using the app",
                Icon = "📅",
                Type = AchievementType.Special,
                Rarity = RarityLevel.Epic,
                CriteriaJson = "{\"type\":\"daysSinceJoin\",\"value\":365}",
                RewardAmount = 20.00m,
                Points = 500,
                SortOrder = 72
            },

            // Interest & Investment Achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "INTEREST_EARNED",
                Title = "Interest Earned",
                Description = "Earn your first interest payment",
                Icon = "📈",
                Type = AchievementType.Savings,
                Rarity = RarityLevel.Rare,
                CriteriaJson = "{\"type\":\"interestPayments\",\"value\":1}",
                RewardAmount = 2.00m,
                Points = 75,
                SortOrder = 80
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Code = "COMPOUND_MASTER",
                Title = "Compound Interest Master",
                Description = "Earn $25 total in interest",
                Icon = "💹",
                Type = AchievementType.Savings,
                Rarity = RarityLevel.Epic,
                CriteriaJson = "{\"type\":\"totalInterest\",\"value\":25}",
                RewardAmount = 10.00m,
                Points = 300,
                SortOrder = 81
            }
        };
    }
}
```

---

## Phase 2: Service Layer (TDD)

### 2.1 IAchievementService Interface

```csharp
namespace AllowanceTracker.Services;

public interface IAchievementService
{
    // Achievement Management
    Task<List<Achievement>> GetAllAchievementsAsync();
    Task<Achievement> GetAchievementByCodeAsync(string code);

    // Child Achievement Tracking
    Task<List<ChildAchievement>> GetChildAchievementsAsync(Guid childId);
    Task<List<Achievement>> GetUnlockedAchievementsAsync(Guid childId);
    Task<List<Achievement>> GetLockedAchievementsAsync(Guid childId);
    Task<AchievementProgress> GetAchievementProgressAsync(Guid childId, string achievementCode);

    // Achievement Unlocking
    Task<ChildAchievement?> CheckAndUnlockAchievementAsync(Guid childId, string achievementCode);
    Task<List<ChildAchievement>> CheckAllAchievementsAsync(Guid childId);
    Task<ChildAchievement> ClaimAchievementRewardAsync(Guid childAchievementId, Guid userId);

    // Statistics & Leaderboard
    Task<AchievementStatistics> GetStatisticsAsync(Guid childId);
    Task<List<LeaderboardEntry>> GetFamilyLeaderboardAsync(Guid familyId);

    // Event Handlers
    Task OnTransactionCreatedAsync(Guid childId, Transaction transaction);
    Task OnSavingsGoalCompletedAsync(Guid childId, WishListItem goal);
    Task OnChoreCompletedAsync(Guid childId, Chore chore);
    Task OnStreakUpdatedAsync(Guid childId, int streakCount);
}
```

### 2.2 Data Transfer Objects

```csharp
namespace AllowanceTracker.DTOs;

public record AchievementProgress(
    Achievement Achievement,
    bool IsUnlocked,
    decimal CurrentValue,
    decimal RequiredValue,
    decimal PercentComplete);

public record AchievementStatistics(
    int TotalAchievements,
    int UnlockedAchievements,
    int LockedAchievements,
    int TotalPoints,
    decimal TotalRewards,
    int CommonBadges,
    int RareBadges,
    int EpicBadges,
    int LegendaryBadges,
    DateTime? LastUnlocked,
    List<Achievement> RecentlyUnlocked);

public record LeaderboardEntry(
    Guid ChildId,
    string ChildName,
    int TotalPoints,
    int AchievementsUnlocked,
    int Rank);
```

### 2.3 AchievementService Implementation

```csharp
namespace AllowanceTracker.Services;

public class AchievementService : IAchievementService
{
    private readonly AllowanceContext _context;
    private readonly ITransactionService _transactionService;
    private readonly IHubContext<FamilyHub> _hubContext;
    private readonly ILogger<AchievementService> _logger;

    public AchievementService(
        AllowanceContext context,
        ITransactionService transactionService,
        IHubContext<FamilyHub> hubContext,
        ILogger<AchievementService> logger)
    {
        _context = context;
        _transactionService = transactionService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<ChildAchievement?> CheckAndUnlockAchievementAsync(
        Guid childId,
        string achievementCode)
    {
        var achievement = await _context.Achievements
            .FirstOrDefaultAsync(a => a.Code == achievementCode && a.IsActive)
            ?? throw new NotFoundException($"Achievement {achievementCode} not found");

        // Check if already unlocked
        var existing = await _context.ChildAchievements
            .FirstOrDefaultAsync(ca => ca.ChildId == childId && ca.AchievementId == achievement.Id);

        if (existing != null)
            return null; // Already unlocked

        var child = await _context.Children
            .Include(c => c.Achievements)
            .FirstOrDefaultAsync(c => c.Id == childId)
            ?? throw new NotFoundException("Child not found");

        // Check criteria
        var criteria = System.Text.Json.JsonSerializer.Deserialize<AchievementCriteria>(
            achievement.CriteriaJson);

        var meetsRequirement = await CheckCriteriaAsync(childId, criteria);

        if (!meetsRequirement)
            return null;

        // Unlock achievement!
        var childAchievement = new ChildAchievement
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            AchievementId = achievement.Id,
            UnlockedAt = DateTime.UtcNow,
            NotificationShown = false,
            RewardClaimed = false
        };

        _context.ChildAchievements.Add(childAchievement);

        // Update child stats
        child.AchievementsUnlocked++;
        child.TotalAchievementPoints += achievement.Points;

        await _context.SaveChangesAsync();

        // Send real-time notification
        await _hubContext.Clients
            .Group($"family-{child.FamilyId}")
            .SendAsync("AchievementUnlocked", childId, achievement.Code, achievement.Title);

        _logger.LogInformation(
            "Child {ChildId} unlocked achievement {Code}: {Title}",
            childId, achievement.Code, achievement.Title);

        return childAchievement;
    }

    private async Task<bool> CheckCriteriaAsync(Guid childId, AchievementCriteria criteria)
    {
        return criteria.Type switch
        {
            "transactionCount" => await CheckTransactionCountAsync(childId, criteria.Value),
            "balance" => await CheckBalanceAsync(childId, criteria.Value),
            "savingsStreak" => await CheckSavingsStreakAsync(childId, (int)criteria.Value),
            "goalsCompleted" => await CheckGoalsCompletedAsync(childId, (int)criteria.Value),
            "choresCompleted" => await CheckChoresCompletedAsync(childId, (int)criteria.Value),
            "achievementCount" => await CheckAchievementCountAsync(childId, (int)criteria.Value),
            "daysSinceJoin" => await CheckDaysSinceJoinAsync(childId, (int)criteria.Value),
            "interestPayments" => await CheckInterestPaymentsAsync(childId, (int)criteria.Value),
            "totalInterest" => await CheckTotalInterestAsync(childId, criteria.Value),
            _ => false
        };
    }

    private async Task<bool> CheckTransactionCountAsync(Guid childId, decimal requiredCount)
    {
        var count = await _context.Transactions
            .CountAsync(t => t.ChildId == childId);

        return count >= requiredCount;
    }

    private async Task<bool> CheckBalanceAsync(Guid childId, decimal requiredBalance)
    {
        var child = await _context.Children.FindAsync(childId);
        return child != null && child.CurrentBalance >= requiredBalance;
    }

    private async Task<bool> CheckSavingsStreakAsync(Guid childId, int requiredStreak)
    {
        var child = await _context.Children.FindAsync(childId);
        return child != null && child.CurrentStreak >= requiredStreak;
    }

    private async Task<bool> CheckGoalsCompletedAsync(Guid childId, int requiredCount)
    {
        var count = await _context.WishListItems
            .CountAsync(g => g.ChildId == childId && g.CompletedAt != null);

        return count >= requiredCount;
    }

    private async Task<bool> CheckChoresCompletedAsync(Guid childId, int requiredCount)
    {
        var count = await _context.Chores
            .CountAsync(c => c.ChildId == childId && c.Status == ChoreStatus.Approved);

        return count >= requiredCount;
    }

    private async Task<bool> CheckAchievementCountAsync(Guid childId, int requiredCount)
    {
        var count = await _context.ChildAchievements
            .CountAsync(ca => ca.ChildId == childId);

        return count >= requiredCount;
    }

    private async Task<bool> CheckDaysSinceJoinAsync(Guid childId, int requiredDays)
    {
        var child = await _context.Children.FindAsync(childId);
        if (child == null) return false;

        var daysSinceJoin = (DateTime.UtcNow - child.CreatedAt).Days;
        return daysSinceJoin >= requiredDays;
    }

    private async Task<bool> CheckInterestPaymentsAsync(Guid childId, int requiredCount)
    {
        var count = await _context.InterestTransactions
            .CountAsync(it => it.ChildId == childId);

        return count >= requiredCount;
    }

    private async Task<bool> CheckTotalInterestAsync(Guid childId, decimal requiredAmount)
    {
        var account = await _context.SavingsAccounts
            .FirstOrDefaultAsync(sa => sa.ChildId == childId);

        return account != null && account.TotalInterestEarned >= requiredAmount;
    }

    public async Task<List<ChildAchievement>> CheckAllAchievementsAsync(Guid childId)
    {
        var allAchievements = await _context.Achievements
            .Where(a => a.IsActive)
            .ToListAsync();

        var newlyUnlocked = new List<ChildAchievement>();

        foreach (var achievement in allAchievements)
        {
            var unlocked = await CheckAndUnlockAchievementAsync(childId, achievement.Code);
            if (unlocked != null)
            {
                newlyUnlocked.Add(unlocked);
            }
        }

        return newlyUnlocked;
    }

    public async Task<ChildAchievement> ClaimAchievementRewardAsync(
        Guid childAchievementId,
        Guid userId)
    {
        var childAchievement = await _context.ChildAchievements
            .Include(ca => ca.Achievement)
            .Include(ca => ca.Child)
            .FirstOrDefaultAsync(ca => ca.Id == childAchievementId)
            ?? throw new NotFoundException("Achievement not found");

        if (childAchievement.RewardClaimed)
            throw new InvalidOperationException("Reward already claimed");

        if (!childAchievement.Achievement.RewardAmount.HasValue ||
            childAchievement.Achievement.RewardAmount.Value <= 0)
            throw new InvalidOperationException("This achievement has no reward");

        // Create transaction for reward
        var transactionDto = new CreateTransactionDto(
            childAchievement.ChildId,
            childAchievement.Achievement.RewardAmount.Value,
            TransactionType.Credit,
            TransactionCategory.BonusReward,
            $"Achievement reward: {childAchievement.Achievement.Title}");

        var transaction = await _transactionService.CreateTransactionAsync(transactionDto);

        childAchievement.RewardClaimed = true;
        childAchievement.RewardTransactionId = transaction.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Child {ChildId} claimed reward for achievement {Code}",
            childAchievement.ChildId, childAchievement.Achievement.Code);

        return childAchievement;
    }

    public async Task<AchievementStatistics> GetStatisticsAsync(Guid childId)
    {
        var allAchievements = await _context.Achievements
            .Where(a => a.IsActive)
            .CountAsync();

        var childAchievements = await _context.ChildAchievements
            .Include(ca => ca.Achievement)
            .Where(ca => ca.ChildId == childId)
            .ToListAsync();

        var child = await _context.Children.FindAsync(childId);

        var totalRewards = childAchievements
            .Where(ca => ca.RewardClaimed && ca.Achievement.RewardAmount.HasValue)
            .Sum(ca => ca.Achievement.RewardAmount!.Value);

        var recentlyUnlocked = childAchievements
            .OrderByDescending(ca => ca.UnlockedAt)
            .Take(5)
            .Select(ca => ca.Achievement)
            .ToList();

        var rarityStats = childAchievements
            .GroupBy(ca => ca.Achievement.Rarity)
            .ToDictionary(g => g.Key, g => g.Count());

        return new AchievementStatistics(
            allAchievements,
            childAchievements.Count,
            allAchievements - childAchievements.Count,
            child?.TotalAchievementPoints ?? 0,
            totalRewards,
            rarityStats.GetValueOrDefault(RarityLevel.Common, 0),
            rarityStats.GetValueOrDefault(RarityLevel.Rare, 0),
            rarityStats.GetValueOrDefault(RarityLevel.Epic, 0),
            rarityStats.GetValueOrDefault(RarityLevel.Legendary, 0),
            childAchievements.Any() ? childAchievements.Max(ca => ca.UnlockedAt) : null,
            recentlyUnlocked);
    }

    public async Task<List<LeaderboardEntry>> GetFamilyLeaderboardAsync(Guid familyId)
    {
        var children = await _context.Children
            .Include(c => c.User)
            .Where(c => c.FamilyId == familyId)
            .OrderByDescending(c => c.TotalAchievementPoints)
            .ThenByDescending(c => c.AchievementsUnlocked)
            .ToListAsync();

        var leaderboard = children
            .Select((child, index) => new LeaderboardEntry(
                child.Id,
                child.FirstName,
                child.TotalAchievementPoints,
                child.AchievementsUnlocked,
                index + 1))
            .ToList();

        return leaderboard;
    }

    // Event handlers
    public async Task OnTransactionCreatedAsync(Guid childId, Transaction transaction)
    {
        await CheckAndUnlockAchievementAsync(childId, "FIRST_TRANSACTION");
        await CheckAndUnlockAchievementAsync(childId, "BALANCE_10");
        await CheckAndUnlockAchievementAsync(childId, "BALANCE_50");
        await CheckAndUnlockAchievementAsync(childId, "BALANCE_100");
        await CheckAndUnlockAchievementAsync(childId, "BALANCE_250");
        await CheckAndUnlockAchievementAsync(childId, "BALANCE_500");
    }

    public async Task OnSavingsGoalCompletedAsync(Guid childId, WishListItem goal)
    {
        await CheckAndUnlockAchievementAsync(childId, "GOAL_COMPLETE_1");
        await CheckAndUnlockAchievementAsync(childId, "GOAL_COMPLETE_5");

        if (goal.TargetAmount >= 100)
        {
            await CheckAndUnlockAchievementAsync(childId, "GOAL_BIG_SAVER");
        }
    }

    public async Task OnChoreCompletedAsync(Guid childId, Chore chore)
    {
        await CheckAndUnlockAchievementAsync(childId, "FIRST_CHORE");
        await CheckAndUnlockAchievementAsync(childId, "CHORES_10");
        await CheckAndUnlockAchievementAsync(childId, "CHORES_50");
        await CheckAndUnlockAchievementAsync(childId, "CHORES_100");
    }

    public async Task OnStreakUpdatedAsync(Guid childId, int streakCount)
    {
        await CheckAndUnlockAchievementAsync(childId, "STREAK_3");
        await CheckAndUnlockAchievementAsync(childId, "STREAK_10");
        await CheckAndUnlockAchievementAsync(childId, "STREAK_20");
    }
}

// Helper class for criteria deserialization
public class AchievementCriteria
{
    public string Type { get; set; } = "";
    public decimal Value { get; set; }
}
```

### 2.4 Test Cases (50 Tests)

Due to space constraints, I'll show a representative sample of the 50 tests:

```csharp
namespace AllowanceTracker.Tests.Services;

public class AchievementServiceTests
{
    // Unlock Achievement Tests (10)
    [Fact]
    public async Task UnlockAchievement_FirstTransaction_UnlocksSuccessfully()
    {
        // Arrange
        var child = await CreateChild();
        await CreateTransaction(child.Id, 10m);

        // Act
        var achievement = await _achievementService.CheckAndUnlockAchievementAsync(
            child.Id, "FIRST_TRANSACTION");

        // Assert
        achievement.Should().NotBeNull();
        achievement!.AchievementId.Should().NotBeEmpty();
        achievement.UnlockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.AchievementsUnlocked.Should().Be(1);
        updatedChild.TotalAchievementPoints.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UnlockAchievement_AlreadyUnlocked_ReturnsNull()
    {
        // Arrange
        var child = await CreateChild();
        await CreateTransaction(child.Id, 10m);
        await _achievementService.CheckAndUnlockAchievementAsync(child.Id, "FIRST_TRANSACTION");

        // Act
        var duplicate = await _achievementService.CheckAndUnlockAchievementAsync(
            child.Id, "FIRST_TRANSACTION");

        // Assert
        duplicate.Should().BeNull();
    }

    [Fact]
    public async Task UnlockAchievement_CriteriaNotMet_ReturnsNull()
    {
        // Arrange
        var child = await CreateChild(balance: 5m);

        // Act - Try to unlock $10 balance achievement
        var achievement = await _achievementService.CheckAndUnlockAchievementAsync(
            child.Id, "BALANCE_10");

        // Assert
        achievement.Should().BeNull();
    }

    [Fact]
    public async Task UnlockAchievement_SendsSignalRNotification()
    {
        // Arrange
        var child = await CreateChild();
        await CreateTransaction(child.Id, 10m);

        // Act
        await _achievementService.CheckAndUnlockAchievementAsync(child.Id, "FIRST_TRANSACTION");

        // Assert
        _mockHubContext.Verify(
            h => h.Clients.Group($"family-{child.FamilyId}")
                .SendAsync("AchievementUnlocked", child.Id, "FIRST_TRANSACTION", It.IsAny<string>(), default),
            Times.Once);
    }

    // Continue with remaining 46 tests covering:
    // - Balance achievements (5 tests)
    // - Streak achievements (5 tests)
    // - Goal achievements (5 tests)
    // - Chore achievements (5 tests)
    // - Collection achievements (5 tests)
    // - Reward claiming (5 tests)
    // - Statistics (5 tests)
    // - Leaderboard (5 tests)
    // - Event handlers (10 tests)
}
```

---

## Phase 3: API Controllers

### 3.1 AchievementsController

```csharp
[ApiController]
[Route("api/v1/achievements")]
[Authorize]
public class AchievementsController : ControllerBase
{
    private readonly IAchievementService _achievementService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Get all available achievements
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Achievement>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Achievement>>> GetAllAchievements()
    {
        var achievements = await _achievementService.GetAllAchievementsAsync();
        return Ok(achievements);
    }

    /// <summary>
    /// Get child's earned achievements
    /// </summary>
    [HttpGet("child/{childId}/unlocked")]
    [ProducesResponseType(typeof(List<Achievement>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Achievement>>> GetUnlockedAchievements(Guid childId)
    {
        var achievements = await _achievementService.GetUnlockedAchievementsAsync(childId);
        return Ok(achievements);
    }

    /// <summary>
    /// Get achievements child hasn't earned yet
    /// </summary>
    [HttpGet("child/{childId}/locked")]
    [ProducesResponseType(typeof(List<Achievement>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Achievement>>> GetLockedAchievements(Guid childId)
    {
        var achievements = await _achievementService.GetLockedAchievementsAsync(childId);
        return Ok(achievements);
    }

    /// <summary>
    /// Get achievement statistics
    /// </summary>
    [HttpGet("child/{childId}/statistics")]
    [ProducesResponseType(typeof(AchievementStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<AchievementStatistics>> GetStatistics(Guid childId)
    {
        var stats = await _achievementService.GetStatisticsAsync(childId);
        return Ok(stats);
    }

    /// <summary>
    /// Claim achievement reward
    /// </summary>
    [HttpPost("{childAchievementId}/claim-reward")]
    [ProducesResponseType(typeof(ChildAchievement), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChildAchievement>> ClaimReward(Guid childAchievementId)
    {
        var userId = _currentUserService.GetUserId();
        var claimed = await _achievementService.ClaimAchievementRewardAsync(childAchievementId, userId);
        return Ok(claimed);
    }

    /// <summary>
    /// Get family leaderboard
    /// </summary>
    [HttpGet("leaderboard/family/{familyId}")]
    [ProducesResponseType(typeof(List<LeaderboardEntry>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LeaderboardEntry>>> GetFamilyLeaderboard(Guid familyId)
    {
        var leaderboard = await _achievementService.GetFamilyLeaderboardAsync(familyId);
        return Ok(leaderboard);
    }

    /// <summary>
    /// Manually trigger achievement check (for testing/admin)
    /// </summary>
    [HttpPost("child/{childId}/check-all")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(List<ChildAchievement>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChildAchievement>>> CheckAllAchievements(Guid childId)
    {
        var newAchievements = await _achievementService.CheckAllAchievementsAsync(childId);
        return Ok(newAchievements);
    }
}
```

---

## Phase 4: Blazor UI Components

### 4.1 BadgeGallery Component

```razor
@page "/achievements/{ChildId:guid}"
@inject IAchievementService AchievementService

<div class="badge-gallery">
    <h3>Achievement Gallery</h3>

    @if (Statistics != null)
    {
        <div class="row mb-4">
            <div class="col-md-3">
                <div class="stat-card">
                    <div class="stat-value">@Statistics.UnlockedAchievements / @Statistics.TotalAchievements</div>
                    <div class="stat-label">Achievements</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card">
                    <div class="stat-value">@Statistics.TotalPoints</div>
                    <div class="stat-label">Total Points</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card">
                    <div class="stat-value">@Statistics.TotalRewards.ToString("C")</div>
                    <div class="stat-label">Total Rewards</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card">
                    <div class="stat-value">@Statistics.LegendaryBadges</div>
                    <div class="stat-label">Legendary</div>
                </div>
            </div>
        </div>
    }

    <!-- Filter by rarity -->
    <div class="mb-3">
        <button class="btn @(selectedRarity == null ? "btn-primary" : "btn-outline-primary")"
                @onclick="() => FilterByRarity(null)">
            All
        </button>
        <button class="btn @(selectedRarity == RarityLevel.Common ? "btn-success" : "btn-outline-success")"
                @onclick="() => FilterByRarity(RarityLevel.Common)">
            Common
        </button>
        <button class="btn @(selectedRarity == RarityLevel.Rare ? "btn-info" : "btn-outline-info")"
                @onclick="() => FilterByRarity(RarityLevel.Rare)">
            Rare
        </button>
        <button class="btn @(selectedRarity == RarityLevel.Epic ? "btn-warning" : "btn-outline-warning")"
                @onclick="() => FilterByRarity(RarityLevel.Epic)">
            Epic
        </button>
        <button class="btn @(selectedRarity == RarityLevel.Legendary ? "btn-danger" : "btn-outline-danger")"
                @onclick="() => FilterByRarity(RarityLevel.Legendary)">
            Legendary
        </button>
    </div>

    <!-- Badge grid -->
    <div class="row">
        @foreach (var achievement in FilteredAchievements)
        {
            <div class="col-md-3 col-sm-4 col-6 mb-4">
                <BadgeCard Achievement="@achievement"
                          IsUnlocked="@IsUnlocked(achievement)"
                          ChildAchievement="@GetChildAchievement(achievement)"
                          OnClaimReward="@RefreshGallery" />
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }

    private List<Achievement> AllAchievements = new();
    private List<Achievement> FilteredAchievements = new();
    private List<ChildAchievement> EarnedAchievements = new();
    private AchievementStatistics? Statistics;
    private RarityLevel? selectedRarity = null;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        AllAchievements = await AchievementService.GetAllAchievementsAsync();
        EarnedAchievements = await AchievementService.GetChildAchievementsAsync(ChildId);
        Statistics = await AchievementService.GetStatisticsAsync(ChildId);

        FilterAchievements();
    }

    private void FilterByRarity(RarityLevel? rarity)
    {
        selectedRarity = rarity;
        FilterAchievements();
    }

    private void FilterAchievements()
    {
        FilteredAchievements = selectedRarity.HasValue
            ? AllAchievements.Where(a => a.Rarity == selectedRarity.Value).ToList()
            : AllAchievements;
    }

    private bool IsUnlocked(Achievement achievement)
    {
        return EarnedAchievements.Any(ca => ca.AchievementId == achievement.Id);
    }

    private ChildAchievement? GetChildAchievement(Achievement achievement)
    {
        return EarnedAchievements.FirstOrDefault(ca => ca.AchievementId == achievement.Id);
    }

    private async Task RefreshGallery()
    {
        await LoadData();
    }
}
```

### 4.2 BadgeCard Component

```razor
<div class="badge-card @GetRarityClass() @(IsUnlocked ? "" : "locked")">
    <div class="badge-icon">
        @if (IsUnlocked)
        {
            <span class="icon-large">@Achievement.Icon</span>
        }
        else
        {
            <span class="icon-large locked-icon">🔒</span>
        }
    </div>

    <div class="badge-title">@Achievement.Title</div>
    <div class="badge-description">@Achievement.Description</div>

    <div class="badge-footer">
        <span class="badge-points">@Achievement.Points pts</span>
        @if (Achievement.RewardAmount.HasValue && Achievement.RewardAmount > 0)
        {
            <span class="badge-reward">@Achievement.RewardAmount.Value.ToString("C")</span>
        }
    </div>

    @if (IsUnlocked && ChildAchievement != null)
    {
        <div class="unlocked-date">
            Unlocked: @ChildAchievement.UnlockedAt.ToString("MMM dd, yyyy")
        </div>

        @if (Achievement.RewardAmount.HasValue && !ChildAchievement.RewardClaimed)
        {
            <button class="btn btn-sm btn-success w-100 mt-2" @onclick="ClaimReward">
                Claim Reward
            </button>
        }
    }
</div>

@code {
    [Parameter] public Achievement Achievement { get; set; } = null!;
    [Parameter] public bool IsUnlocked { get; set; }
    [Parameter] public ChildAchievement? ChildAchievement { get; set; }
    [Parameter] public EventCallback OnClaimReward { get; set; }

    [Inject] private IAchievementService AchievementService { get; set; } = null!;

    private string GetRarityClass()
    {
        return Achievement.Rarity switch
        {
            RarityLevel.Common => "rarity-common",
            RarityLevel.Rare => "rarity-rare",
            RarityLevel.Epic => "rarity-epic",
            RarityLevel.Legendary => "rarity-legendary",
            _ => ""
        };
    }

    private async Task ClaimReward()
    {
        if (ChildAchievement == null) return;

        await AchievementService.ClaimAchievementRewardAsync(ChildAchievement.Id, Guid.Empty);
        await OnClaimReward.InvokeAsync();
    }
}
```

---

## Success Metrics

- All 50 tests passing
- 30+ achievements defined and seeded
- Achievement unlocking automated via events
- Badge gallery displays correctly
- Rarity filtering functional
- Reward claiming working
- Leaderboard calculates correctly
- Real-time unlock notifications

---

## Future Enhancements

1. **Secret Achievements**: Hidden achievements discovered by exploring
2. **Time-Limited Events**: Seasonal/holiday achievements
3. **Achievement Chains**: Unlock series of related achievements
4. **Custom Family Achievements**: Parents create custom badges
5. **Social Sharing**: Share achievements on social media
6. **Achievement Trading**: Trade duplicate badges (if implemented)
7. **Profile Showcase**: Display favorite badges on profile

---

**Total Implementation Time**: 4-5 weeks following TDD methodology
