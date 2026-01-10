# Achievement Badges System Specification

## Overview

The Achievement Badges system provides gamification to encourage positive financial behaviors. Children earn badges for accomplishments like saving consistently, completing tasks, reaching goals, and maintaining streaks. Badges come with points that can be redeemed for rewards like custom avatars and themes.

Key features:
- 30+ predefined badges across multiple categories
- Progress tracking toward unearned badges
- Point system with redeemable rewards
- Badge showcase on child profiles
- Event-driven badge unlocking

---

## Database Schema

### Badge Model

```csharp
public class Badge
{
    public Guid Id { get; set; }

    // Badge details
    public string Code { get; set; } = string.Empty;  // Unique identifier like "FIRST_SAVER"
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;

    // Classification
    public BadgeCategory Category { get; set; }
    public BadgeRarity Rarity { get; set; }

    // Value
    public int PointsValue { get; set; }

    // Unlock criteria (JSON for complex conditions)
    public BadgeCriteriaType CriteriaType { get; set; }
    public string CriteriaConfig { get; set; } = "{}";  // JSON configuration

    // Display
    public bool IsSecret { get; set; } = false;  // Hidden until earned
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual ICollection<ChildBadge> ChildBadges { get; set; } = new List<ChildBadge>();
}

public enum BadgeCategory
{
    Saving = 1,
    Spending = 2,
    Goals = 3,
    Chores = 4,
    Streaks = 5,
    Milestones = 6,
    Special = 7
}

public enum BadgeRarity
{
    Common = 1,      // Easy to earn
    Uncommon = 2,    // Moderate effort
    Rare = 3,        // Significant achievement
    Epic = 4,        // Major accomplishment
    Legendary = 5    // Exceptional achievement
}

public enum BadgeCriteriaType
{
    SingleAction = 1,       // One-time action (first save, first purchase)
    CountThreshold = 2,     // Reach a count (10 tasks, 50 transactions)
    AmountThreshold = 3,    // Reach an amount ($100 saved, $500 earned)
    StreakCount = 4,        // Maintain streak (4 weeks saving)
    PercentageTarget = 5,   // Hit percentage (save 50% of allowance)
    GoalCompletion = 6,     // Complete goals (1, 5, 10 goals)
    TimeBasedAction = 7,    // Time-specific (save same day as allowance)
    Compound = 8            // Multiple conditions
}
```

### ChildBadge Model

```csharp
public class ChildBadge
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid BadgeId { get; set; }
    public virtual Badge Badge { get; set; } = null!;

    public DateTime EarnedAt { get; set; }
    public bool IsDisplayed { get; set; } = true;  // Show on profile
    public bool IsNew { get; set; } = true;        // Unseen by user

    // Context of how it was earned
    public string? EarnedContext { get; set; }  // JSON with details
}
```

### BadgeProgress Model

```csharp
public class BadgeProgress
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid BadgeId { get; set; }
    public virtual Badge Badge { get; set; } = null!;

    public int CurrentProgress { get; set; }
    public int TargetProgress { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Reward Model

```csharp
public class Reward
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RewardType Type { get; set; }
    public string Value { get; set; } = string.Empty;  // URL or identifier
    public string? PreviewUrl { get; set; }

    public int PointsCost { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}

public enum RewardType
{
    Avatar = 1,
    Theme = 2,
    Title = 3,
    ProfileFrame = 4,
    Special = 5
}
```

### ChildReward Model

```csharp
public class ChildReward
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid RewardId { get; set; }
    public virtual Reward Reward { get; set; } = null!;

    public DateTime UnlockedAt { get; set; }
    public bool IsEquipped { get; set; } = false;
}
```

### Child Model Updates

```csharp
// Add to existing Child model
public class Child
{
    // ... existing properties ...

    // Achievement system
    public int TotalPoints { get; set; } = 0;
    public int AvailablePoints { get; set; } = 0;  // Points available to spend
    public string? EquippedAvatarUrl { get; set; }
    public string? EquippedTheme { get; set; }
    public string? EquippedTitle { get; set; }

    // Navigation
    public virtual ICollection<ChildBadge> Badges { get; set; } = new List<ChildBadge>();
    public virtual ICollection<BadgeProgress> BadgeProgress { get; set; } = new List<BadgeProgress>();
    public virtual ICollection<ChildReward> Rewards { get; set; } = new List<ChildReward>();
}
```

---

## Badge Definitions

### Saving Badges

| Code | Name | Description | Rarity | Points | Criteria |
|------|------|-------------|--------|--------|----------|
| FIRST_SAVER | First Saver | Made your first deposit to savings | Common | 10 | SingleAction: first savings deposit |
| PENNY_PINCHER | Penny Pincher | Saved $10 total | Common | 15 | AmountThreshold: $10 total saved |
| MONEY_STACKER | Money Stacker | Saved $50 total | Uncommon | 25 | AmountThreshold: $50 total saved |
| SAVINGS_STAR | Savings Star | Saved $100 total | Rare | 50 | AmountThreshold: $100 total saved |
| SAVINGS_CHAMPION | Savings Champion | Saved $500 total | Epic | 100 | AmountThreshold: $500 total saved |
| EARLY_BIRD | Early Bird | Saved on the same day as allowance | Uncommon | 20 | TimeBasedAction: save on allowance day |
| SUPER_SAVER | Super Saver | Saved 50% of allowance in a month | Rare | 40 | PercentageTarget: 50% savings rate |
| FRUGAL_MASTER | Frugal Master | Saved 75% of allowance in a month | Epic | 75 | PercentageTarget: 75% savings rate |

### Goals Badges

| Code | Name | Description | Rarity | Points | Criteria |
|------|------|-------------|--------|--------|----------|
| GOAL_SETTER | Goal Setter | Created your first savings goal | Common | 10 | SingleAction: first goal created |
| GOAL_CRUSHER | Goal Crusher | Completed your first savings goal | Common | 20 | GoalCompletion: 1 goal |
| DREAM_ACHIEVER | Dream Achiever | Completed 5 savings goals | Rare | 50 | GoalCompletion: 5 goals |
| GOAL_MACHINE | Goal Machine | Completed 10 savings goals | Epic | 100 | GoalCompletion: 10 goals |
| WISHLIST_WINNER | Wishlist Winner | Purchased an item from your wishlist | Common | 15 | SingleAction: first wishlist purchase |

### Chores Badges

| Code | Name | Description | Rarity | Points | Criteria |
|------|------|-------------|--------|--------|----------|
| HELPER | Helper | Completed your first task | Common | 10 | SingleAction: first task completed |
| HARD_WORKER | Hard Worker | Completed 10 tasks | Common | 20 | CountThreshold: 10 tasks |
| CHORE_CHAMPION | Chore Champion | Completed 50 tasks | Rare | 50 | CountThreshold: 50 tasks |
| TASK_MASTER | Task Master | Completed 100 tasks | Epic | 100 | CountThreshold: 100 tasks |
| PERFECT_RECORD | Perfect Record | Had 10 tasks approved in a row | Rare | 40 | StreakCount: 10 approved tasks |

### Streaks Badges

| Code | Name | Description | Rarity | Points | Criteria |
|------|------|-------------|--------|--------|----------|
| STREAK_STARTER | Streak Starter | Saved for 2 weeks in a row | Common | 15 | StreakCount: 2-week saving streak |
| CONSISTENCY_KING | Consistency King | Saved for 4 weeks in a row | Uncommon | 30 | StreakCount: 4-week saving streak |
| STREAK_MASTER | Streak Master | Saved for 10 weeks in a row | Rare | 60 | StreakCount: 10-week saving streak |
| UNSTOPPABLE | Unstoppable | Saved for 26 weeks in a row | Epic | 100 | StreakCount: 26-week saving streak |
| LEGENDARY_STREAK | Legendary Streak | Saved for 52 weeks in a row | Legendary | 200 | StreakCount: 52-week saving streak |

### Milestones Badges

| Code | Name | Description | Rarity | Points | Criteria |
|------|------|-------------|--------|--------|----------|
| FIRST_PURCHASE | First Purchase | Made your first transaction | Common | 5 | SingleAction: first transaction |
| DOUBLE_DIGITS | Double Digits | Reached $10 balance | Common | 10 | AmountThreshold: $10 balance |
| FIFTY_CLUB | Fifty Club | Reached $50 balance | Uncommon | 25 | AmountThreshold: $50 balance |
| CENTURY_CLUB | Century Club | Reached $100 balance | Rare | 50 | AmountThreshold: $100 balance |
| HIGH_ROLLER | High Roller | Reached $500 balance | Epic | 100 | AmountThreshold: $500 balance |

### Spending Badges

| Code | Name | Description | Rarity | Points | Criteria |
|------|------|-------------|--------|--------|----------|
| BUDGET_AWARE | Budget Aware | Stayed under budget for a week | Common | 15 | StreakCount: 1-week under budget |
| BUDGET_BOSS | Budget Boss | Stayed under budget for 4 weeks | Rare | 50 | StreakCount: 4-week under budget |
| SMART_SPENDER | Smart Spender | Tracked 50 transactions | Uncommon | 25 | CountThreshold: 50 transactions |
| TRANSACTION_TRACKER | Transaction Tracker | Tracked 200 transactions | Rare | 50 | CountThreshold: 200 transactions |

### Special Badges

| Code | Name | Description | Rarity | Points | Criteria |
|------|------|-------------|--------|--------|----------|
| WELCOME | Welcome | Joined the app | Common | 5 | SingleAction: account created |
| BIRTHDAY_BONUS | Birthday Bonus | Received a gift on your birthday | Uncommon | 25 | Special: birthday gift |
| GENEROUS_HEART | Generous Heart | Gave money to a sibling | Rare | 40 | SingleAction: sibling transfer |
| FAMILY_FIRST | Family First | Part of a family savings goal | Rare | 40 | Special: family goal participant |

---

## DTOs

### Request DTOs

```csharp
// Toggle badge display on profile
public record UpdateBadgeDisplayDto(
    bool IsDisplayed
);

// Unlock a reward with points
public record UnlockRewardDto(
    Guid RewardId
);

// Equip a reward
public record EquipRewardDto(
    Guid RewardId
);

// Mark badges as seen
public record MarkBadgesSeenDto(
    List<Guid> BadgeIds
);
```

### Response DTOs

```csharp
public record BadgeDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string IconUrl,
    BadgeCategory Category,
    string CategoryName,
    BadgeRarity Rarity,
    string RarityName,
    int PointsValue,
    bool IsSecret,
    bool IsEarned,
    DateTime? EarnedAt,
    bool IsDisplayed,
    int? CurrentProgress,
    int? TargetProgress,
    double? ProgressPercentage
);

public record ChildBadgeDto(
    Guid Id,
    Guid BadgeId,
    string BadgeName,
    string BadgeDescription,
    string IconUrl,
    BadgeCategory Category,
    BadgeRarity Rarity,
    int PointsValue,
    DateTime EarnedAt,
    bool IsDisplayed,
    bool IsNew,
    string? EarnedContext
);

public record BadgeProgressDto(
    Guid BadgeId,
    string BadgeName,
    string Description,
    string IconUrl,
    BadgeCategory Category,
    BadgeRarity Rarity,
    int PointsValue,
    int CurrentProgress,
    int TargetProgress,
    double ProgressPercentage,
    string ProgressText  // e.g., "7/10 tasks completed"
);

public record ChildPointsDto(
    int TotalPoints,
    int AvailablePoints,
    int SpentPoints,
    int BadgesEarned,
    int RewardsUnlocked
);

public record RewardDto(
    Guid Id,
    string Name,
    string Description,
    RewardType Type,
    string TypeName,
    string Value,
    string? PreviewUrl,
    int PointsCost,
    bool IsUnlocked,
    bool IsEquipped,
    bool CanAfford
);

public record BadgeUnlockedEventDto(
    Guid ChildId,
    BadgeDto Badge,
    int NewTotalPoints,
    int NewAvailablePoints
);

public record AchievementSummaryDto(
    int TotalBadges,
    int EarnedBadges,
    int TotalPoints,
    int AvailablePoints,
    List<ChildBadgeDto> RecentBadges,
    List<BadgeProgressDto> InProgressBadges,
    Dictionary<BadgeCategory, int> BadgesByCategory
);
```

---

## API Endpoints

### Badges

#### GET /api/v1/badges
Get all available badges

**Authorization**: Authenticated user
**Response**: `List<BadgeDto>`

**Query Parameters**:
- `category` (optional) - Filter by category
- `includeSecret` (default: false) - Include secret badges

---

#### GET /api/v1/children/{childId}/badges
Get child's earned badges

**Authorization**: Parent or self
**Response**: `List<ChildBadgeDto>`

**Query Parameters**:
- `category` (optional) - Filter by category
- `newOnly` (default: false) - Only return unseen badges

---

#### GET /api/v1/children/{childId}/badges/progress
Get progress toward unearned badges

**Authorization**: Parent or self
**Response**: `List<BadgeProgressDto>`

---

#### GET /api/v1/children/{childId}/badges/summary
Get achievement summary

**Authorization**: Parent or self
**Response**: `AchievementSummaryDto`

---

#### PATCH /api/v1/children/{childId}/badges/{badgeId}/display
Toggle badge display on profile

**Authorization**: Self only
**Request Body**: `UpdateBadgeDisplayDto`
**Response**: `ChildBadgeDto`

---

#### POST /api/v1/children/{childId}/badges/seen
Mark badges as seen

**Authorization**: Self only
**Request Body**: `MarkBadgesSeenDto`
**Response**: 204 No Content

---

### Points & Rewards

#### GET /api/v1/children/{childId}/points
Get child's points summary

**Authorization**: Parent or self
**Response**: `ChildPointsDto`

---

#### GET /api/v1/rewards
Get available rewards

**Authorization**: Authenticated user
**Response**: `List<RewardDto>`

**Query Parameters**:
- `type` (optional) - Filter by reward type
- `affordable` (optional) - Only show affordable rewards

---

#### GET /api/v1/children/{childId}/rewards
Get child's unlocked rewards

**Authorization**: Parent or self
**Response**: `List<RewardDto>`

---

#### POST /api/v1/children/{childId}/rewards/{rewardId}/unlock
Unlock reward with points

**Authorization**: Parent or self
**Response**: `RewardDto`

**Business Rules**:
- Must have enough available points
- Cannot unlock same reward twice
- Deducts points from available balance

---

#### POST /api/v1/children/{childId}/rewards/{rewardId}/equip
Equip an unlocked reward

**Authorization**: Self only
**Response**: `RewardDto`

**Business Rules**:
- Must have unlocked the reward
- Only one of each type can be equipped

---

#### POST /api/v1/children/{childId}/rewards/{rewardId}/unequip
Unequip a reward

**Authorization**: Self only
**Response**: 204 No Content

---

## Service Layer

### IAchievementService

```csharp
public interface IAchievementService
{
    // Badge queries
    Task<List<BadgeDto>> GetAllBadgesAsync(BadgeCategory? category, bool includeSecret);
    Task<List<ChildBadgeDto>> GetChildBadgesAsync(Guid childId, BadgeCategory? category, bool newOnly);
    Task<List<BadgeProgressDto>> GetBadgeProgressAsync(Guid childId);
    Task<AchievementSummaryDto> GetAchievementSummaryAsync(Guid childId);

    // Badge mutations
    Task<ChildBadgeDto> ToggleBadgeDisplayAsync(Guid childId, Guid badgeId, bool isDisplayed);
    Task MarkBadgesSeenAsync(Guid childId, List<Guid> badgeIds);

    // Badge unlocking (called by event handlers)
    Task<ChildBadgeDto?> TryUnlockBadgeAsync(Guid childId, string badgeCode, string? context = null);
    Task CheckAndUnlockBadgesAsync(Guid childId, BadgeTrigger trigger, object? triggerData = null);

    // Points
    Task<ChildPointsDto> GetChildPointsAsync(Guid childId);

    // Rewards
    Task<List<RewardDto>> GetAvailableRewardsAsync(RewardType? type, Guid? childId);
    Task<List<RewardDto>> GetChildRewardsAsync(Guid childId);
    Task<RewardDto> UnlockRewardAsync(Guid childId, Guid rewardId);
    Task<RewardDto> EquipRewardAsync(Guid childId, Guid rewardId);
    Task UnequipRewardAsync(Guid childId, Guid rewardId);

    // Progress tracking
    Task UpdateProgressAsync(Guid childId, string badgeCode, int increment = 1);
    Task SetProgressAsync(Guid childId, string badgeCode, int value);
}

public enum BadgeTrigger
{
    TransactionCreated,
    SavingsDeposit,
    GoalCreated,
    GoalCompleted,
    TaskCompleted,
    TaskApproved,
    AllowanceReceived,
    BalanceChanged,
    StreakUpdated,
    BudgetChecked
}
```

### IBadgeCriteriaEvaluator

```csharp
public interface IBadgeCriteriaEvaluator
{
    Task<bool> EvaluateAsync(Badge badge, Guid childId, object? triggerData);
    Task<(int current, int target)> GetProgressAsync(Badge badge, Guid childId);
}

public class BadgeCriteriaEvaluator : IBadgeCriteriaEvaluator
{
    public async Task<bool> EvaluateAsync(Badge badge, Guid childId, object? triggerData)
    {
        var config = JsonSerializer.Deserialize<BadgeCriteriaConfig>(badge.CriteriaConfig);

        return badge.CriteriaType switch
        {
            BadgeCriteriaType.SingleAction => await EvaluateSingleActionAsync(config, childId, triggerData),
            BadgeCriteriaType.CountThreshold => await EvaluateCountThresholdAsync(config, childId),
            BadgeCriteriaType.AmountThreshold => await EvaluateAmountThresholdAsync(config, childId),
            BadgeCriteriaType.StreakCount => await EvaluateStreakCountAsync(config, childId),
            BadgeCriteriaType.PercentageTarget => await EvaluatePercentageTargetAsync(config, childId),
            BadgeCriteriaType.GoalCompletion => await EvaluateGoalCompletionAsync(config, childId),
            BadgeCriteriaType.TimeBasedAction => await EvaluateTimeBasedActionAsync(config, childId, triggerData),
            BadgeCriteriaType.Compound => await EvaluateCompoundAsync(config, childId, triggerData),
            _ => false
        };
    }
}
```

---

## Event-Driven Badge Unlocking

### Badge Triggers

Badges are checked automatically when relevant events occur:

```csharp
// In TransactionService
public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto, Guid userId)
{
    // ... create transaction ...

    // Trigger badge checks
    await _achievementService.CheckAndUnlockBadgesAsync(
        dto.ChildId,
        BadgeTrigger.TransactionCreated,
        new { Transaction = transaction }
    );

    if (dto.Type == TransactionType.Credit && dto.IsSavingsDeposit)
    {
        await _achievementService.CheckAndUnlockBadgesAsync(
            dto.ChildId,
            BadgeTrigger.SavingsDeposit,
            new { Amount = dto.Amount }
        );
    }

    return result;
}

// In TaskService
public async Task<TaskCompletionDto> ReviewCompletionAsync(Guid completionId, ReviewCompletionDto dto, Guid userId)
{
    // ... approve task ...

    if (dto.IsApproved)
    {
        await _achievementService.CheckAndUnlockBadgesAsync(
            completion.ChildId,
            BadgeTrigger.TaskApproved,
            new { Task = completion.Task, Completion = completion }
        );
    }

    return result;
}
```

### Badge Check Implementation

```csharp
public async Task CheckAndUnlockBadgesAsync(Guid childId, BadgeTrigger trigger, object? triggerData)
{
    // Get badges that respond to this trigger
    var relevantBadges = await GetBadgesForTriggerAsync(trigger);

    // Get badges child hasn't earned yet
    var earnedBadgeIds = await _context.ChildBadges
        .Where(cb => cb.ChildId == childId)
        .Select(cb => cb.BadgeId)
        .ToListAsync();

    var unearnedBadges = relevantBadges.Where(b => !earnedBadgeIds.Contains(b.Id));

    foreach (var badge in unearnedBadges)
    {
        var isEarned = await _criteriaEvaluator.EvaluateAsync(badge, childId, triggerData);

        if (isEarned)
        {
            await UnlockBadgeAsync(childId, badge, triggerData);
        }
        else
        {
            // Update progress for progress-based badges
            await UpdateBadgeProgressAsync(childId, badge);
        }
    }
}

private async Task UnlockBadgeAsync(Guid childId, Badge badge, object? triggerData)
{
    var childBadge = new ChildBadge
    {
        ChildId = childId,
        BadgeId = badge.Id,
        EarnedAt = DateTime.UtcNow,
        IsNew = true,
        EarnedContext = triggerData != null ? JsonSerializer.Serialize(triggerData) : null
    };

    _context.ChildBadges.Add(childBadge);

    // Award points
    var child = await _context.Children.FindAsync(childId);
    child!.TotalPoints += badge.PointsValue;
    child.AvailablePoints += badge.PointsValue;

    await _context.SaveChangesAsync();

    // Send notification
    await _notificationService.SendNotificationAsync(
        child.UserId,
        NotificationType.AchievementUnlocked,
        "Achievement Unlocked!",
        $"You earned the \"{badge.Name}\" badge! +{badge.PointsValue} points",
        new { BadgeId = badge.Id, BadgeName = badge.Name, Points = badge.PointsValue }
    );
}
```

---

## iOS Implementation

### Models

```swift
import Foundation

struct Badge: Codable, Identifiable {
    let id: UUID
    let code: String
    let name: String
    let description: String
    let iconUrl: String
    let category: BadgeCategory
    let categoryName: String
    let rarity: BadgeRarity
    let rarityName: String
    let pointsValue: Int
    let isSecret: Bool
    let isEarned: Bool
    let earnedAt: Date?
    let isDisplayed: Bool
    let currentProgress: Int?
    let targetProgress: Int?
    let progressPercentage: Double?
}

struct ChildBadge: Codable, Identifiable {
    let id: UUID
    let badgeId: UUID
    let badgeName: String
    let badgeDescription: String
    let iconUrl: String
    let category: BadgeCategory
    let rarity: BadgeRarity
    let pointsValue: Int
    let earnedAt: Date
    let isDisplayed: Bool
    let isNew: Bool
}

struct BadgeProgress: Codable, Identifiable {
    var id: UUID { badgeId }
    let badgeId: UUID
    let badgeName: String
    let description: String
    let iconUrl: String
    let category: BadgeCategory
    let rarity: BadgeRarity
    let pointsValue: Int
    let currentProgress: Int
    let targetProgress: Int
    let progressPercentage: Double
    let progressText: String
}

struct Reward: Codable, Identifiable {
    let id: UUID
    let name: String
    let description: String
    let type: RewardType
    let typeName: String
    let value: String
    let previewUrl: String?
    let pointsCost: Int
    let isUnlocked: Bool
    let isEquipped: Bool
    let canAfford: Bool
}

struct AchievementSummary: Codable {
    let totalBadges: Int
    let earnedBadges: Int
    let totalPoints: Int
    let availablePoints: Int
    let recentBadges: [ChildBadge]
    let inProgressBadges: [BadgeProgress]
    let badgesByCategory: [String: Int]
}

enum BadgeCategory: Int, Codable, CaseIterable {
    case saving = 1
    case spending = 2
    case goals = 3
    case chores = 4
    case streaks = 5
    case milestones = 6
    case special = 7

    var displayName: String {
        switch self {
        case .saving: return "Saving"
        case .spending: return "Spending"
        case .goals: return "Goals"
        case .chores: return "Chores"
        case .streaks: return "Streaks"
        case .milestones: return "Milestones"
        case .special: return "Special"
        }
    }

    var iconName: String {
        switch self {
        case .saving: return "banknote"
        case .spending: return "cart"
        case .goals: return "target"
        case .chores: return "checklist"
        case .streaks: return "flame"
        case .milestones: return "flag"
        case .special: return "star"
        }
    }
}

enum BadgeRarity: Int, Codable, CaseIterable {
    case common = 1
    case uncommon = 2
    case rare = 3
    case epic = 4
    case legendary = 5

    var displayName: String {
        switch self {
        case .common: return "Common"
        case .uncommon: return "Uncommon"
        case .rare: return "Rare"
        case .epic: return "Epic"
        case .legendary: return "Legendary"
        }
    }

    var color: Color {
        switch self {
        case .common: return .gray
        case .uncommon: return .green
        case .rare: return .blue
        case .epic: return .purple
        case .legendary: return .orange
        }
    }
}

enum RewardType: Int, Codable {
    case avatar = 1
    case theme = 2
    case title = 3
    case profileFrame = 4
    case special = 5
}
```

### ViewModel

```swift
import Foundation

@Observable
@MainActor
final class AchievementViewModel {
    var summary: AchievementSummary?
    var allBadges: [Badge] = []
    var earnedBadges: [ChildBadge] = []
    var inProgressBadges: [BadgeProgress] = []
    var rewards: [Reward] = []
    var unlockedRewards: [Reward] = []

    var isLoading = false
    var errorMessage: String?

    private let childId: UUID
    private let apiService: APIServiceProtocol

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.apiService = apiService
    }

    func loadSummary() async {
        isLoading = true
        errorMessage = nil

        do {
            summary = try await apiService.get(
                endpoint: "/api/v1/children/\(childId)/badges/summary"
            )
        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func loadAllBadges(category: BadgeCategory? = nil) async {
        do {
            var endpoint = "/api/v1/badges"
            if let category = category {
                endpoint += "?category=\(category.rawValue)"
            }
            allBadges = try await apiService.get(endpoint: endpoint)
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadEarnedBadges() async {
        do {
            earnedBadges = try await apiService.get(
                endpoint: "/api/v1/children/\(childId)/badges"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadProgress() async {
        do {
            inProgressBadges = try await apiService.get(
                endpoint: "/api/v1/children/\(childId)/badges/progress"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadRewards() async {
        do {
            rewards = try await apiService.get(
                endpoint: "/api/v1/rewards?childId=\(childId)"
            )
            unlockedRewards = try await apiService.get(
                endpoint: "/api/v1/children/\(childId)/rewards"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func unlockReward(_ reward: Reward) async -> Bool {
        do {
            let _: Reward = try await apiService.post(
                endpoint: "/api/v1/children/\(childId)/rewards/\(reward.id)/unlock",
                body: EmptyBody()
            )
            await loadRewards()
            await loadSummary()
            return true
        } catch {
            errorMessage = error.localizedDescription
            return false
        }
    }

    func equipReward(_ reward: Reward) async {
        do {
            let _: Reward = try await apiService.post(
                endpoint: "/api/v1/children/\(childId)/rewards/\(reward.id)/equip",
                body: EmptyBody()
            )
            await loadRewards()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func toggleBadgeDisplay(_ badge: ChildBadge) async {
        do {
            let _: ChildBadge = try await apiService.patch(
                endpoint: "/api/v1/children/\(childId)/badges/\(badge.badgeId)/display",
                body: ["isDisplayed": !badge.isDisplayed]
            )
            await loadEarnedBadges()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func markBadgesSeen() async {
        let newBadgeIds = earnedBadges.filter { $0.isNew }.map { $0.id }
        guard !newBadgeIds.isEmpty else { return }

        do {
            try await apiService.post(
                endpoint: "/api/v1/children/\(childId)/badges/seen",
                body: ["badgeIds": newBadgeIds]
            )
        } catch {
            // Silently fail - not critical
        }
    }
}

struct EmptyBody: Codable {}
```

### Views

```swift
import SwiftUI

struct AchievementsView: View {
    let childId: UUID
    @State private var viewModel: AchievementViewModel
    @State private var selectedTab = 0

    init(childId: UUID) {
        self.childId = childId
        self._viewModel = State(initialValue: AchievementViewModel(childId: childId))
    }

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                // Points header
                if let summary = viewModel.summary {
                    PointsHeaderView(summary: summary)
                }

                // Tab selector
                Picker("View", selection: $selectedTab) {
                    Text("Badges").tag(0)
                    Text("Progress").tag(1)
                    Text("Rewards").tag(2)
                }
                .pickerStyle(.segmented)
                .padding()

                // Content
                TabView(selection: $selectedTab) {
                    BadgesListView(badges: viewModel.earnedBadges, onToggleDisplay: { badge in
                        Task { await viewModel.toggleBadgeDisplay(badge) }
                    })
                    .tag(0)

                    ProgressListView(progress: viewModel.inProgressBadges)
                        .tag(1)

                    RewardsListView(
                        rewards: viewModel.rewards,
                        unlockedRewards: viewModel.unlockedRewards,
                        availablePoints: viewModel.summary?.availablePoints ?? 0,
                        onUnlock: { reward in
                            Task { await viewModel.unlockReward(reward) }
                        },
                        onEquip: { reward in
                            Task { await viewModel.equipReward(reward) }
                        }
                    )
                    .tag(2)
                }
                .tabViewStyle(.page(indexDisplayMode: .never))
            }
            .navigationTitle("Achievements")
            .refreshable {
                await loadAll()
            }
        }
        .task {
            await loadAll()
        }
        .onDisappear {
            Task { await viewModel.markBadgesSeen() }
        }
    }

    private func loadAll() async {
        await viewModel.loadSummary()
        await viewModel.loadEarnedBadges()
        await viewModel.loadProgress()
        await viewModel.loadRewards()
    }
}

struct PointsHeaderView: View {
    let summary: AchievementSummary

    var body: some View {
        HStack(spacing: 24) {
            VStack {
                Text("\(summary.earnedBadges)")
                    .font(.title)
                    .fontWeight(.bold)
                Text("Badges")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Divider()
                .frame(height: 40)

            VStack {
                Text("\(summary.totalPoints)")
                    .font(.title)
                    .fontWeight(.bold)
                    .foregroundStyle(.purple)
                Text("Total Points")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Divider()
                .frame(height: 40)

            VStack {
                Text("\(summary.availablePoints)")
                    .font(.title)
                    .fontWeight(.bold)
                    .foregroundStyle(.green)
                Text("Available")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding()
        .background(Color(.systemBackground))
    }
}

struct BadgesListView: View {
    let badges: [ChildBadge]
    let onToggleDisplay: (ChildBadge) -> Void

    var body: some View {
        ScrollView {
            LazyVGrid(columns: [
                GridItem(.flexible()),
                GridItem(.flexible()),
                GridItem(.flexible())
            ], spacing: 16) {
                ForEach(badges) { badge in
                    BadgeCardView(badge: badge, onToggleDisplay: {
                        onToggleDisplay(badge)
                    })
                }
            }
            .padding()
        }
    }
}

struct BadgeCardView: View {
    let badge: ChildBadge
    let onToggleDisplay: () -> Void

    var body: some View {
        VStack(spacing: 8) {
            ZStack {
                Circle()
                    .fill(badge.rarity.color.opacity(0.2))
                    .frame(width: 70, height: 70)

                AsyncImage(url: URL(string: badge.iconUrl)) { image in
                    image.resizable().aspectRatio(contentMode: .fit)
                } placeholder: {
                    Image(systemName: "medal.fill")
                        .font(.title)
                }
                .frame(width: 40, height: 40)

                if badge.isNew {
                    Circle()
                        .fill(.red)
                        .frame(width: 12, height: 12)
                        .offset(x: 25, y: -25)
                }
            }

            Text(badge.badgeName)
                .font(.caption)
                .fontWeight(.medium)
                .multilineTextAlignment(.center)
                .lineLimit(2)

            Text("+\(badge.pointsValue) pts")
                .font(.caption2)
                .foregroundStyle(.secondary)
        }
        .padding(8)
        .background(Color(.secondarySystemBackground))
        .cornerRadius(12)
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(badge.rarity.color, lineWidth: 2)
        )
    }
}

struct ProgressListView: View {
    let progress: [BadgeProgress]

    var body: some View {
        List(progress) { item in
            HStack(spacing: 12) {
                AsyncImage(url: URL(string: item.iconUrl)) { image in
                    image.resizable().aspectRatio(contentMode: .fit)
                } placeholder: {
                    Image(systemName: "medal")
                }
                .frame(width: 40, height: 40)
                .opacity(0.5)

                VStack(alignment: .leading, spacing: 4) {
                    Text(item.badgeName)
                        .font(.headline)

                    Text(item.progressText)
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    ProgressView(value: item.progressPercentage / 100)
                        .tint(item.rarity.color)
                }

                Spacer()

                Text("\(Int(item.progressPercentage))%")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
            .padding(.vertical, 4)
        }
        .listStyle(.plain)
    }
}

struct RewardsListView: View {
    let rewards: [Reward]
    let unlockedRewards: [Reward]
    let availablePoints: Int
    let onUnlock: (Reward) -> Void
    let onEquip: (Reward) -> Void

    var body: some View {
        List {
            Section("Available Rewards") {
                ForEach(rewards.filter { !$0.isUnlocked }) { reward in
                    RewardRow(
                        reward: reward,
                        availablePoints: availablePoints,
                        onUnlock: { onUnlock(reward) }
                    )
                }
            }

            Section("Unlocked") {
                ForEach(unlockedRewards) { reward in
                    UnlockedRewardRow(reward: reward, onEquip: { onEquip(reward) })
                }
            }
        }
        .listStyle(.insetGrouped)
    }
}

struct RewardRow: View {
    let reward: Reward
    let availablePoints: Int
    let onUnlock: () -> Void

    var body: some View {
        HStack {
            if let previewUrl = reward.previewUrl {
                AsyncImage(url: URL(string: previewUrl)) { image in
                    image.resizable().aspectRatio(contentMode: .fit)
                } placeholder: {
                    Color.gray.opacity(0.3)
                }
                .frame(width: 50, height: 50)
                .cornerRadius(8)
            }

            VStack(alignment: .leading) {
                Text(reward.name)
                    .font(.headline)
                Text(reward.description)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            Button(action: onUnlock) {
                HStack {
                    Image(systemName: "star.fill")
                    Text("\(reward.pointsCost)")
                }
                .font(.caption)
                .padding(.horizontal, 12)
                .padding(.vertical, 6)
                .background(reward.canAfford ? Color.purple : Color.gray)
                .foregroundColor(.white)
                .cornerRadius(16)
            }
            .disabled(!reward.canAfford)
        }
    }
}

struct UnlockedRewardRow: View {
    let reward: Reward
    let onEquip: () -> Void

    var body: some View {
        HStack {
            if let previewUrl = reward.previewUrl {
                AsyncImage(url: URL(string: previewUrl)) { image in
                    image.resizable().aspectRatio(contentMode: .fit)
                } placeholder: {
                    Color.gray.opacity(0.3)
                }
                .frame(width: 50, height: 50)
                .cornerRadius(8)
            }

            VStack(alignment: .leading) {
                Text(reward.name)
                    .font(.headline)
                Text(reward.typeName)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            if reward.isEquipped {
                Text("Equipped")
                    .font(.caption)
                    .foregroundStyle(.green)
            } else {
                Button("Equip", action: onEquip)
                    .font(.caption)
            }
        }
    }
}
```

---

## Testing Strategy

### Unit Tests - 50 tests

```csharp
public class AchievementServiceTests
{
    // Badge queries (8 tests)
    [Fact]
    public async Task GetAllBadges_ReturnsActiveBadges() { }

    [Fact]
    public async Task GetChildBadges_ReturnsEarnedBadges() { }

    [Fact]
    public async Task GetBadgeProgress_ReturnsInProgressBadges() { }

    // Badge unlocking (15 tests)
    [Fact]
    public async Task TryUnlockBadge_UnlocksWhenCriteriaMet() { }

    [Fact]
    public async Task TryUnlockBadge_ReturnsNullWhenAlreadyEarned() { }

    [Fact]
    public async Task CheckAndUnlockBadges_UnlocksMultipleBadges() { }

    [Fact]
    public async Task UnlockBadge_AwardsPoints() { }

    [Fact]
    public async Task UnlockBadge_SendsNotification() { }

    // Criteria evaluation (12 tests)
    [Fact]
    public async Task EvaluateSingleAction_ReturnsTrueOnFirstAction() { }

    [Fact]
    public async Task EvaluateCountThreshold_ReturnsTrueWhenThresholdReached() { }

    [Fact]
    public async Task EvaluateAmountThreshold_ReturnsTrueWhenAmountReached() { }

    [Fact]
    public async Task EvaluateStreakCount_ReturnsTrueWhenStreakMet() { }

    // Progress tracking (8 tests)
    [Fact]
    public async Task UpdateProgress_IncrementsProgress() { }

    [Fact]
    public async Task UpdateProgress_DoesNotExceedTarget() { }

    // Rewards (7 tests)
    [Fact]
    public async Task UnlockReward_DeductsPoints() { }

    [Fact]
    public async Task UnlockReward_FailsWithInsufficientPoints() { }

    [Fact]
    public async Task EquipReward_SetsEquippedFlag() { }

    [Fact]
    public async Task EquipReward_UnequipsPreviousOfSameType() { }
}
```

### Integration Tests - 10 tests

```csharp
public class AchievementIntegrationTests
{
    [Fact]
    public async Task FirstSavingsDeposit_UnlocksFirstSaverBadge() { }

    [Fact]
    public async Task TenTasksCompleted_UnlocksHardWorkerBadge() { }

    [Fact]
    public async Task BalanceReaches100_UnlocksCenturyClubBadge() { }

    [Fact]
    public async Task FourWeekSavingStreak_UnlocksConsistencyKingBadge() { }

    [Fact]
    public async Task CompletingFirstGoal_UnlocksGoalCrusherBadge() { }
}
```

---

## Implementation Phases

### Phase 1: Database & Models (2 days)
- [ ] Create Badge, ChildBadge, BadgeProgress models
- [ ] Create Reward, ChildReward models
- [ ] Add points fields to Child model
- [ ] Create database migration
- [ ] Seed initial badge definitions (30 badges)
- [ ] Seed initial rewards

### Phase 2: Achievement Service (3 days)
- [ ] Write IAchievementService tests
- [ ] Implement badge query methods
- [ ] Implement badge unlock logic
- [ ] Implement IBadgeCriteriaEvaluator
- [ ] Implement progress tracking

### Phase 3: Points & Rewards (2 days)
- [ ] Implement points calculation
- [ ] Implement reward unlock logic
- [ ] Implement reward equip/unequip

### Phase 4: Event Integration (3 days)
- [ ] Add badge triggers to TransactionService
- [ ] Add badge triggers to TaskService
- [ ] Add badge triggers to AllowanceService
- [ ] Add badge triggers to GoalService
- [ ] Integrate with notification system

### Phase 5: API Controllers (2 days)
- [ ] Write controller tests
- [ ] Implement AchievementsController
- [ ] Implement RewardsController

### Phase 6: iOS Implementation (3 days)
- [ ] Create Swift models
- [ ] Create AchievementViewModel
- [ ] Create AchievementsView
- [ ] Create badge unlock animation
- [ ] Integrate with profile

### Phase 7: React Implementation (2 days)
- [ ] Create TypeScript types
- [ ] Create useAchievements hook
- [ ] Create AchievementsPage component
- [ ] Create BadgeCard component
- [ ] Create RewardShop component

---

## Success Criteria

- [ ] 30 predefined badges seeded in database
- [ ] Badges unlock automatically on relevant events
- [ ] Points awarded correctly on badge unlock
- [ ] Progress tracked for threshold-based badges
- [ ] Rewards can be purchased with points
- [ ] Equipped rewards display on profile
- [ ] Notification sent on badge unlock
- [ ] >90% test coverage on AchievementService

---

This specification provides a complete gamification system following TDD principles and the existing AllowanceTracker architecture.
