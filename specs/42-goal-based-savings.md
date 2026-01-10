# Goal-Based Savings System Specification

## Overview

The Goal-Based Savings system transforms passive wish lists into active savings journeys. Children create savings goals with visual progress tracking, parents can offer matching contributions, and time-bound challenges add excitement. Celebration moments reward achievement.

Key features:
- Visual progress tracking with milestones (25%, 50%, 75%, 100%)
- Parent matching ("I'll add $1 for every $2 you save")
- Time-bound savings challenges with bonus rewards
- Auto-transfer from allowance to goals
- Celebration animations when goals are reached

---

## Database Schema

### SavingsGoal Model

```csharp
public class SavingsGoal
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    // Goal details
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; } = 0;
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public GoalCategory Category { get; set; } = GoalCategory.Other;

    // Deadline
    public DateTime? TargetDate { get; set; }

    // Status
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public DateTime? CompletedAt { get; set; }
    public DateTime? PurchasedAt { get; set; }

    // Priority (for ordering)
    public int Priority { get; set; } = 1;

    // Auto-transfer settings
    public decimal AutoTransferAmount { get; set; } = 0;
    public AutoTransferType AutoTransferType { get; set; } = AutoTransferType.None;

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<SavingsContribution> Contributions { get; set; } = new List<SavingsContribution>();
    public virtual ICollection<GoalMilestone> Milestones { get; set; } = new List<GoalMilestone>();
    public virtual ParentMatchingRule? MatchingRule { get; set; }
    public virtual GoalChallenge? ActiveChallenge { get; set; }
}

public enum GoalStatus
{
    Active = 1,
    Completed = 2,      // Target reached
    Purchased = 3,      // Item purchased
    Cancelled = 4,
    Paused = 5
}

public enum GoalCategory
{
    Toy = 1,
    Game = 2,
    Electronics = 3,
    Clothing = 4,
    Experience = 5,     // Movie, trip, etc.
    Savings = 6,        // General savings
    Charity = 7,
    Other = 99
}

public enum AutoTransferType
{
    None = 0,
    FixedAmount = 1,    // Transfer $X per allowance
    Percentage = 2       // Transfer X% of allowance
}
```

### SavingsContribution Model

```csharp
public class SavingsContribution
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }
    public virtual SavingsGoal Goal { get; set; } = null!;

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public decimal Amount { get; set; }
    public ContributionType Type { get; set; }
    public decimal GoalBalanceAfter { get; set; }

    // Source tracking
    public Guid? SourceTransactionId { get; set; }  // If from main balance
    public Guid? ParentMatchId { get; set; }        // If this is a match contribution

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
}

public enum ContributionType
{
    ChildDeposit = 1,       // Manual deposit by child
    AutoTransfer = 2,       // Automatic from allowance
    ParentMatch = 3,        // Parent matching contribution
    ParentGift = 4,         // Direct parent contribution
    ChallengeBonus = 5,     // Bonus for completing challenge
    Withdrawal = 6,         // Withdrawal (negative amount)
    ExternalGift = 7        // Gift from extended family
}
```

### ParentMatchingRule Model

```csharp
public class ParentMatchingRule
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }
    public virtual SavingsGoal Goal { get; set; } = null!;

    public Guid CreatedByParentId { get; set; }
    public virtual ApplicationUser CreatedByParent { get; set; } = null!;

    // Matching configuration
    public MatchingType Type { get; set; }
    public decimal MatchRatio { get; set; }         // e.g., 0.5 = $1 for every $2
    public decimal? MaxMatchAmount { get; set; }    // Cap on total matching

    // Tracking
    public decimal TotalMatchedAmount { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public enum MatchingType
{
    RatioMatch = 1,         // Add $1 for every $X saved
    PercentageMatch = 2,    // Match X% of each deposit
    MilestoneBonus = 3      // Bonus at milestone completion
}
```

### GoalMilestone Model

```csharp
public class GoalMilestone
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }
    public virtual SavingsGoal Goal { get; set; } = null!;

    public int PercentComplete { get; set; }  // 25, 50, 75, 100
    public decimal TargetAmount { get; set; }

    public bool IsAchieved { get; set; } = false;
    public DateTime? AchievedAt { get; set; }

    public string? CelebrationMessage { get; set; }
    public decimal? BonusAmount { get; set; }  // Optional bonus at milestone
}
```

### GoalChallenge Model

```csharp
public class GoalChallenge
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }
    public virtual SavingsGoal Goal { get; set; } = null!;

    public Guid CreatedByParentId { get; set; }

    // Challenge parameters
    public decimal TargetAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal BonusAmount { get; set; }

    // Status
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Active;
    public DateTime? CompletedAt { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum ChallengeStatus
{
    Active = 1,
    Completed = 2,      // Target reached in time
    Failed = 3,         // Deadline passed
    Cancelled = 4
}
```

---

## DTOs

### Request DTOs

```csharp
public record CreateSavingsGoalDto(
    string Name,
    string? Description,
    decimal TargetAmount,
    string? ImageUrl,
    string? ProductUrl,
    GoalCategory Category,
    DateTime? TargetDate,
    int Priority,
    decimal AutoTransferAmount,
    AutoTransferType AutoTransferType
);

public record UpdateSavingsGoalDto(
    string? Name,
    string? Description,
    decimal? TargetAmount,
    string? ImageUrl,
    string? ProductUrl,
    GoalCategory? Category,
    DateTime? TargetDate,
    int? Priority,
    decimal? AutoTransferAmount,
    AutoTransferType? AutoTransferType
);

public record ContributeToGoalDto(
    decimal Amount,
    string? Description
);

public record WithdrawFromGoalDto(
    decimal Amount,
    string? Reason
);

public record CreateMatchingRuleDto(
    MatchingType Type,
    decimal MatchRatio,
    decimal? MaxMatchAmount,
    DateTime? ExpiresAt
);

public record UpdateMatchingRuleDto(
    decimal? MatchRatio,
    decimal? MaxMatchAmount,
    bool? IsActive,
    DateTime? ExpiresAt
);

public record CreateGoalChallengeDto(
    decimal TargetAmount,
    DateTime EndDate,
    decimal BonusAmount,
    string? Description
);

public record MarkGoalPurchasedDto(
    string? Notes
);
```

### Response DTOs

```csharp
public record SavingsGoalDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    string Name,
    string? Description,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    double ProgressPercentage,
    string? ImageUrl,
    string? ProductUrl,
    GoalCategory Category,
    string CategoryName,
    DateTime? TargetDate,
    int? DaysRemaining,
    GoalStatus Status,
    string StatusName,
    DateTime? CompletedAt,
    int Priority,
    decimal AutoTransferAmount,
    AutoTransferType AutoTransferType,
    bool HasMatchingRule,
    MatchingRuleSummaryDto? MatchingRule,
    bool HasActiveChallenge,
    ChallengeSummaryDto? ActiveChallenge,
    List<MilestoneDto> Milestones,
    DateTime CreatedAt
);

public record SavingsGoalSummaryDto(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    double ProgressPercentage,
    string? ImageUrl,
    GoalCategory Category,
    DateTime? TargetDate,
    GoalStatus Status,
    bool CanAfford  // Based on child's current balance
);

public record ContributionDto(
    Guid Id,
    decimal Amount,
    ContributionType Type,
    string TypeName,
    decimal GoalBalanceAfter,
    string? Description,
    DateTime CreatedAt,
    string? CreatedByName
);

public record MatchingRuleSummaryDto(
    Guid Id,
    MatchingType Type,
    string TypeDescription,  // e.g., "$1 for every $2 saved"
    decimal MatchRatio,
    decimal? MaxMatchAmount,
    decimal TotalMatchedAmount,
    decimal? RemainingMatchAmount,
    bool IsActive,
    DateTime? ExpiresAt
);

public record MatchingRuleDto(
    Guid Id,
    Guid GoalId,
    string GoalName,
    MatchingType Type,
    string TypeDescription,
    decimal MatchRatio,
    decimal? MaxMatchAmount,
    decimal TotalMatchedAmount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string CreatedByName
);

public record ChallengeSummaryDto(
    Guid Id,
    decimal TargetAmount,
    decimal CurrentProgress,
    double ProgressPercentage,
    DateTime EndDate,
    int DaysRemaining,
    decimal BonusAmount,
    ChallengeStatus Status
);

public record GoalChallengeDto(
    Guid Id,
    Guid GoalId,
    string GoalName,
    decimal TargetAmount,
    decimal CurrentProgress,
    double ProgressPercentage,
    DateTime StartDate,
    DateTime EndDate,
    int DaysRemaining,
    decimal BonusAmount,
    ChallengeStatus Status,
    string StatusName,
    DateTime? CompletedAt,
    string? Description
);

public record MilestoneDto(
    Guid Id,
    int PercentComplete,
    decimal TargetAmount,
    bool IsAchieved,
    DateTime? AchievedAt,
    string? CelebrationMessage,
    decimal? BonusAmount
);

public record GoalProgressEventDto(
    Guid GoalId,
    string GoalName,
    decimal NewAmount,
    decimal TargetAmount,
    double ProgressPercentage,
    MilestoneDto? MilestoneReached,
    bool IsCompleted,
    decimal? MatchAmountAdded
);
```

---

## API Endpoints

### Savings Goals

#### POST /api/v1/savings-goals
Create a new savings goal

**Authorization**: Parent or Child
**Request Body**: `CreateSavingsGoalDto`
**Response**: `SavingsGoalDto`

---

#### GET /api/v1/savings-goals/{id}
Get goal details

**Authorization**: Family member
**Response**: `SavingsGoalDto`

---

#### GET /api/v1/children/{childId}/savings-goals
Get child's savings goals

**Authorization**: Parent or self
**Response**: `List<SavingsGoalDto>`

**Query Parameters**:
- `status` (optional) - Filter by status
- `includeCompleted` (default: false) - Include completed/purchased goals

---

#### PUT /api/v1/savings-goals/{id}
Update goal details

**Authorization**: Parent or goal owner
**Request Body**: `UpdateSavingsGoalDto`
**Response**: `SavingsGoalDto`

---

#### DELETE /api/v1/savings-goals/{id}
Cancel a goal (returns funds to balance)

**Authorization**: Parent
**Response**: 204 No Content

---

#### POST /api/v1/savings-goals/{id}/pause
Pause a goal

**Authorization**: Parent
**Response**: `SavingsGoalDto`

---

#### POST /api/v1/savings-goals/{id}/resume
Resume a paused goal

**Authorization**: Parent
**Response**: `SavingsGoalDto`

---

### Contributions

#### POST /api/v1/savings-goals/{id}/contribute
Contribute to a goal from child's balance

**Authorization**: Parent or self
**Request Body**: `ContributeToGoalDto`
**Response**: `GoalProgressEventDto`

**Business Rules**:
- Deducts from child's main balance
- Creates contribution record
- Checks and applies parent matching
- Checks milestone achievements
- Checks goal completion

---

#### POST /api/v1/savings-goals/{id}/withdraw
Withdraw from a goal back to balance

**Authorization**: Parent only
**Request Body**: `WithdrawFromGoalDto`
**Response**: `ContributionDto`

---

#### GET /api/v1/savings-goals/{id}/contributions
Get contribution history

**Authorization**: Family member
**Response**: `List<ContributionDto>`

**Query Parameters**:
- `type` (optional) - Filter by contribution type
- `startDate` / `endDate` (optional) - Date range

---

#### POST /api/v1/savings-goals/{id}/purchase
Mark goal as purchased

**Authorization**: Parent
**Request Body**: `MarkGoalPurchasedDto`
**Response**: `SavingsGoalDto`

**Business Rules**:
- Goal must be completed (100% funded)
- Sets status to Purchased
- Deducts goal amount
- Creates purchase transaction

---

### Parent Matching

#### POST /api/v1/savings-goals/{id}/matching
Create matching rule

**Authorization**: Parent only
**Request Body**: `CreateMatchingRuleDto`
**Response**: `MatchingRuleDto`

---

#### GET /api/v1/savings-goals/{id}/matching
Get matching rule

**Authorization**: Family member
**Response**: `MatchingRuleDto`

---

#### PUT /api/v1/savings-goals/{id}/matching
Update matching rule

**Authorization**: Parent only
**Request Body**: `UpdateMatchingRuleDto`
**Response**: `MatchingRuleDto`

---

#### DELETE /api/v1/savings-goals/{id}/matching
Remove matching rule

**Authorization**: Parent only
**Response**: 204 No Content

---

### Challenges

#### POST /api/v1/savings-goals/{id}/challenge
Create time-bound challenge

**Authorization**: Parent only
**Request Body**: `CreateGoalChallengeDto`
**Response**: `GoalChallengeDto`

---

#### GET /api/v1/savings-goals/{id}/challenge
Get active challenge

**Authorization**: Family member
**Response**: `GoalChallengeDto`

---

#### DELETE /api/v1/savings-goals/{id}/challenge
Cancel challenge

**Authorization**: Parent only
**Response**: 204 No Content

---

#### GET /api/v1/children/{childId}/challenges
Get all challenges for child

**Authorization**: Parent or self
**Response**: `List<GoalChallengeDto>`

---

## Service Layer

### ISavingsGoalService

```csharp
public interface ISavingsGoalService
{
    // Goal CRUD
    Task<SavingsGoalDto> CreateGoalAsync(Guid childId, CreateSavingsGoalDto dto, Guid userId);
    Task<SavingsGoalDto> GetGoalByIdAsync(Guid goalId, Guid userId);
    Task<List<SavingsGoalDto>> GetChildGoalsAsync(Guid childId, GoalStatus? status, bool includeCompleted, Guid userId);
    Task<SavingsGoalDto> UpdateGoalAsync(Guid goalId, UpdateSavingsGoalDto dto, Guid userId);
    Task CancelGoalAsync(Guid goalId, Guid userId);
    Task<SavingsGoalDto> PauseGoalAsync(Guid goalId, Guid userId);
    Task<SavingsGoalDto> ResumeGoalAsync(Guid goalId, Guid userId);

    // Contributions
    Task<GoalProgressEventDto> ContributeAsync(Guid goalId, ContributeToGoalDto dto, Guid userId);
    Task<ContributionDto> WithdrawAsync(Guid goalId, WithdrawFromGoalDto dto, Guid userId);
    Task<List<ContributionDto>> GetContributionsAsync(Guid goalId, ContributionType? type, DateTime? startDate, DateTime? endDate);
    Task<SavingsGoalDto> MarkAsPurchasedAsync(Guid goalId, MarkGoalPurchasedDto dto, Guid userId);

    // Auto-transfer (called by AllowanceService)
    Task ProcessAutoTransfersAsync(Guid childId, decimal allowanceAmount);

    // Matching
    Task<MatchingRuleDto> CreateMatchingRuleAsync(Guid goalId, CreateMatchingRuleDto dto, Guid parentId);
    Task<MatchingRuleDto?> GetMatchingRuleAsync(Guid goalId);
    Task<MatchingRuleDto> UpdateMatchingRuleAsync(Guid goalId, UpdateMatchingRuleDto dto, Guid parentId);
    Task RemoveMatchingRuleAsync(Guid goalId, Guid parentId);

    // Challenges
    Task<GoalChallengeDto> CreateChallengeAsync(Guid goalId, CreateGoalChallengeDto dto, Guid parentId);
    Task<GoalChallengeDto?> GetActiveChallengeAsync(Guid goalId);
    Task CancelChallengeAsync(Guid goalId, Guid parentId);
    Task<List<GoalChallengeDto>> GetChildChallengesAsync(Guid childId, Guid userId);

    // Background job
    Task CheckExpiredChallengesAsync();
}
```

### Contribution Flow

```csharp
public async Task<GoalProgressEventDto> ContributeAsync(Guid goalId, ContributeToGoalDto dto, Guid userId)
{
    var goal = await _context.SavingsGoals
        .Include(g => g.Child)
        .Include(g => g.MatchingRule)
        .Include(g => g.Milestones)
        .Include(g => g.ActiveChallenge)
        .FirstOrDefaultAsync(g => g.Id == goalId);

    // Validate
    if (goal == null) throw new NotFoundException("Goal not found");
    if (goal.Status != GoalStatus.Active) throw new InvalidOperationException("Goal is not active");
    if (goal.Child.CurrentBalance < dto.Amount) throw new InvalidOperationException("Insufficient balance");

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 1. Deduct from child's balance
        goal.Child.CurrentBalance -= dto.Amount;

        // 2. Create contribution
        var contribution = new SavingsContribution
        {
            GoalId = goalId,
            ChildId = goal.ChildId,
            Amount = dto.Amount,
            Type = ContributionType.ChildDeposit,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        goal.CurrentAmount += dto.Amount;
        contribution.GoalBalanceAfter = goal.CurrentAmount;
        _context.SavingsContributions.Add(contribution);

        // 3. Apply parent matching
        decimal matchAmount = 0;
        if (goal.MatchingRule?.IsActive == true)
        {
            matchAmount = CalculateMatchAmount(goal.MatchingRule, dto.Amount);
            if (matchAmount > 0)
            {
                var matchContribution = new SavingsContribution
                {
                    GoalId = goalId,
                    ChildId = goal.ChildId,
                    Amount = matchAmount,
                    Type = ContributionType.ParentMatch,
                    ParentMatchId = contribution.Id,
                    Description = $"Parent match for ${dto.Amount} deposit",
                    CreatedAt = DateTime.UtcNow
                };

                goal.CurrentAmount += matchAmount;
                matchContribution.GoalBalanceAfter = goal.CurrentAmount;
                goal.MatchingRule.TotalMatchedAmount += matchAmount;

                _context.SavingsContributions.Add(matchContribution);
            }
        }

        // 4. Check milestones
        MilestoneDto? milestoneReached = null;
        var progressPercent = (goal.CurrentAmount / goal.TargetAmount) * 100;

        foreach (var milestone in goal.Milestones.Where(m => !m.IsAchieved).OrderBy(m => m.PercentComplete))
        {
            if (progressPercent >= milestone.PercentComplete)
            {
                milestone.IsAchieved = true;
                milestone.AchievedAt = DateTime.UtcNow;
                milestoneReached = MapToDto(milestone);

                // Award milestone bonus if configured
                if (milestone.BonusAmount.HasValue && milestone.BonusAmount > 0)
                {
                    var bonusContribution = new SavingsContribution
                    {
                        GoalId = goalId,
                        ChildId = goal.ChildId,
                        Amount = milestone.BonusAmount.Value,
                        Type = ContributionType.ChallengeBonus,
                        Description = $"Bonus for reaching {milestone.PercentComplete}% milestone",
                        CreatedAt = DateTime.UtcNow
                    };
                    goal.CurrentAmount += milestone.BonusAmount.Value;
                    bonusContribution.GoalBalanceAfter = goal.CurrentAmount;
                    _context.SavingsContributions.Add(bonusContribution);
                }

                // Send milestone notification
                await _notificationService.SendNotificationAsync(
                    goal.Child.UserId,
                    NotificationType.GoalMilestone,
                    "Milestone Reached!",
                    $"You've hit {milestone.PercentComplete}% on your {goal.Name} goal!",
                    new { GoalId = goalId, Percent = milestone.PercentComplete }
                );
            }
        }

        // 5. Check goal completion
        bool isCompleted = false;
        if (goal.CurrentAmount >= goal.TargetAmount)
        {
            goal.Status = GoalStatus.Completed;
            goal.CompletedAt = DateTime.UtcNow;
            isCompleted = true;

            // Check challenge completion
            if (goal.ActiveChallenge?.Status == ChallengeStatus.Active)
            {
                goal.ActiveChallenge.Status = ChallengeStatus.Completed;
                goal.ActiveChallenge.CompletedAt = DateTime.UtcNow;

                // Award challenge bonus
                var challengeBonus = new SavingsContribution
                {
                    GoalId = goalId,
                    ChildId = goal.ChildId,
                    Amount = goal.ActiveChallenge.BonusAmount,
                    Type = ContributionType.ChallengeBonus,
                    Description = "Challenge completion bonus!",
                    CreatedAt = DateTime.UtcNow
                };
                goal.CurrentAmount += goal.ActiveChallenge.BonusAmount;
                challengeBonus.GoalBalanceAfter = goal.CurrentAmount;
                _context.SavingsContributions.Add(challengeBonus);
            }

            // Send completion notification
            await _notificationService.SendNotificationAsync(
                goal.Child.UserId,
                NotificationType.GoalCompleted,
                "Goal Achieved!",
                $"Congratulations! You saved enough for {goal.Name}!",
                new { GoalId = goalId, GoalName = goal.Name }
            );

            // Trigger badge checks
            await _achievementService.CheckAndUnlockBadgesAsync(
                goal.ChildId,
                BadgeTrigger.GoalCompleted,
                new { Goal = goal }
            );
        }

        goal.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new GoalProgressEventDto(
            GoalId: goalId,
            GoalName: goal.Name,
            NewAmount: goal.CurrentAmount,
            TargetAmount: goal.TargetAmount,
            ProgressPercentage: (double)(goal.CurrentAmount / goal.TargetAmount * 100),
            MilestoneReached: milestoneReached,
            IsCompleted: isCompleted,
            MatchAmountAdded: matchAmount > 0 ? matchAmount : null
        );
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

private decimal CalculateMatchAmount(ParentMatchingRule rule, decimal depositAmount)
{
    decimal matchAmount = rule.Type switch
    {
        MatchingType.RatioMatch => depositAmount * rule.MatchRatio,
        MatchingType.PercentageMatch => depositAmount * (rule.MatchRatio / 100),
        _ => 0
    };

    // Apply cap if set
    if (rule.MaxMatchAmount.HasValue)
    {
        var remainingCap = rule.MaxMatchAmount.Value - rule.TotalMatchedAmount;
        matchAmount = Math.Min(matchAmount, remainingCap);
    }

    return Math.Max(0, matchAmount);
}
```

---

## iOS Implementation

### Models

```swift
import Foundation

struct SavingsGoal: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let childName: String
    let name: String
    let description: String?
    let targetAmount: Decimal
    let currentAmount: Decimal
    let remainingAmount: Decimal
    let progressPercentage: Double
    let imageUrl: String?
    let productUrl: String?
    let category: GoalCategory
    let categoryName: String
    let targetDate: Date?
    let daysRemaining: Int?
    let status: GoalStatus
    let statusName: String
    let completedAt: Date?
    let priority: Int
    let autoTransferAmount: Decimal
    let autoTransferType: AutoTransferType
    let hasMatchingRule: Bool
    let matchingRule: MatchingRuleSummary?
    let hasActiveChallenge: Bool
    let activeChallenge: ChallengeSummary?
    let milestones: [Milestone]
    let createdAt: Date
}

struct Contribution: Codable, Identifiable {
    let id: UUID
    let amount: Decimal
    let type: ContributionType
    let typeName: String
    let goalBalanceAfter: Decimal
    let description: String?
    let createdAt: Date
    let createdByName: String?
}

struct MatchingRuleSummary: Codable {
    let id: UUID
    let type: MatchingType
    let typeDescription: String
    let matchRatio: Decimal
    let maxMatchAmount: Decimal?
    let totalMatchedAmount: Decimal
    let remainingMatchAmount: Decimal?
    let isActive: Bool
    let expiresAt: Date?
}

struct ChallengeSummary: Codable {
    let id: UUID
    let targetAmount: Decimal
    let currentProgress: Decimal
    let progressPercentage: Double
    let endDate: Date
    let daysRemaining: Int
    let bonusAmount: Decimal
    let status: ChallengeStatus
}

struct Milestone: Codable, Identifiable {
    let id: UUID
    let percentComplete: Int
    let targetAmount: Decimal
    let isAchieved: Bool
    let achievedAt: Date?
    let celebrationMessage: String?
    let bonusAmount: Decimal?
}

struct GoalProgressEvent: Codable {
    let goalId: UUID
    let goalName: String
    let newAmount: Decimal
    let targetAmount: Decimal
    let progressPercentage: Double
    let milestoneReached: Milestone?
    let isCompleted: Bool
    let matchAmountAdded: Decimal?
}

enum GoalStatus: Int, Codable {
    case active = 1
    case completed = 2
    case purchased = 3
    case cancelled = 4
    case paused = 5
}

enum GoalCategory: Int, Codable, CaseIterable {
    case toy = 1
    case game = 2
    case electronics = 3
    case clothing = 4
    case experience = 5
    case savings = 6
    case charity = 7
    case other = 99

    var displayName: String {
        switch self {
        case .toy: return "Toy"
        case .game: return "Game"
        case .electronics: return "Electronics"
        case .clothing: return "Clothing"
        case .experience: return "Experience"
        case .savings: return "Savings"
        case .charity: return "Charity"
        case .other: return "Other"
        }
    }

    var iconName: String {
        switch self {
        case .toy: return "teddybear"
        case .game: return "gamecontroller"
        case .electronics: return "desktopcomputer"
        case .clothing: return "tshirt"
        case .experience: return "ticket"
        case .savings: return "banknote"
        case .charity: return "heart"
        case .other: return "star"
        }
    }
}

enum ContributionType: Int, Codable {
    case childDeposit = 1
    case autoTransfer = 2
    case parentMatch = 3
    case parentGift = 4
    case challengeBonus = 5
    case withdrawal = 6
    case externalGift = 7
}

enum MatchingType: Int, Codable {
    case ratioMatch = 1
    case percentageMatch = 2
    case milestoneBonus = 3
}

enum AutoTransferType: Int, Codable {
    case none = 0
    case fixedAmount = 1
    case percentage = 2
}

enum ChallengeStatus: Int, Codable {
    case active = 1
    case completed = 2
    case failed = 3
    case cancelled = 4
}
```

### ViewModel

```swift
import Foundation

@Observable
@MainActor
final class SavingsGoalViewModel {
    var goals: [SavingsGoal] = []
    var selectedGoal: SavingsGoal?
    var contributions: [Contribution] = []

    var isLoading = false
    var errorMessage: String?
    var showCelebration = false
    var celebrationMessage: String?

    private let childId: UUID
    private let apiService: APIServiceProtocol

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.apiService = apiService
    }

    func loadGoals(includeCompleted: Bool = false) async {
        isLoading = true
        errorMessage = nil

        do {
            goals = try await apiService.get(
                endpoint: "/api/v1/children/\(childId)/savings-goals",
                queryParams: ["includeCompleted": "\(includeCompleted)"]
            )
        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func loadGoalDetails(_ goalId: UUID) async {
        do {
            selectedGoal = try await apiService.get(
                endpoint: "/api/v1/savings-goals/\(goalId)"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadContributions(_ goalId: UUID) async {
        do {
            contributions = try await apiService.get(
                endpoint: "/api/v1/savings-goals/\(goalId)/contributions"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func createGoal(_ request: CreateGoalRequest) async -> SavingsGoal? {
        do {
            let goal: SavingsGoal = try await apiService.post(
                endpoint: "/api/v1/savings-goals",
                body: request
            )
            await loadGoals()
            return goal
        } catch {
            errorMessage = error.localizedDescription
            return nil
        }
    }

    func contribute(to goalId: UUID, amount: Decimal, description: String?) async {
        do {
            let result: GoalProgressEvent = try await apiService.post(
                endpoint: "/api/v1/savings-goals/\(goalId)/contribute",
                body: ContributeRequest(amount: amount, description: description)
            )

            // Handle celebration
            if result.isCompleted {
                celebrationMessage = "Goal Achieved!"
                showCelebration = true
            } else if let milestone = result.milestoneReached {
                celebrationMessage = milestone.celebrationMessage ?? "Milestone reached!"
                showCelebration = true
            }

            await loadGoals()
            await loadGoalDetails(goalId)
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func createMatchingRule(for goalId: UUID, request: CreateMatchingRequest) async {
        do {
            let _: MatchingRule = try await apiService.post(
                endpoint: "/api/v1/savings-goals/\(goalId)/matching",
                body: request
            )
            await loadGoalDetails(goalId)
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func createChallenge(for goalId: UUID, request: CreateChallengeRequest) async {
        do {
            let _: GoalChallenge = try await apiService.post(
                endpoint: "/api/v1/savings-goals/\(goalId)/challenge",
                body: request
            )
            await loadGoalDetails(goalId)
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func markAsPurchased(_ goalId: UUID) async {
        do {
            let _: SavingsGoal = try await apiService.post(
                endpoint: "/api/v1/savings-goals/\(goalId)/purchase",
                body: MarkPurchasedRequest(notes: nil)
            )
            await loadGoals()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}

struct CreateGoalRequest: Codable {
    let name: String
    let description: String?
    let targetAmount: Decimal
    let imageUrl: String?
    let productUrl: String?
    let category: GoalCategory
    let targetDate: Date?
    let priority: Int
    let autoTransferAmount: Decimal
    let autoTransferType: AutoTransferType
}

struct ContributeRequest: Codable {
    let amount: Decimal
    let description: String?
}

struct CreateMatchingRequest: Codable {
    let type: MatchingType
    let matchRatio: Decimal
    let maxMatchAmount: Decimal?
    let expiresAt: Date?
}

struct CreateChallengeRequest: Codable {
    let targetAmount: Decimal
    let endDate: Date
    let bonusAmount: Decimal
    let description: String?
}

struct MarkPurchasedRequest: Codable {
    let notes: String?
}
```

### Views

```swift
import SwiftUI

struct SavingsGoalsView: View {
    let childId: UUID
    @State private var viewModel: SavingsGoalViewModel
    @State private var showingCreateGoal = false

    init(childId: UUID) {
        self.childId = childId
        self._viewModel = State(initialValue: SavingsGoalViewModel(childId: childId))
    }

    var body: some View {
        NavigationStack {
            Group {
                if viewModel.isLoading && viewModel.goals.isEmpty {
                    ProgressView("Loading goals...")
                } else if viewModel.goals.isEmpty {
                    EmptyGoalsView(onCreateGoal: { showingCreateGoal = true })
                } else {
                    goalsList
                }
            }
            .navigationTitle("Savings Goals")
            .toolbar {
                ToolbarItem(placement: .primaryAction) {
                    Button(action: { showingCreateGoal = true }) {
                        Image(systemName: "plus")
                    }
                }
            }
            .sheet(isPresented: $showingCreateGoal) {
                CreateGoalView(viewModel: viewModel)
            }
            .refreshable {
                await viewModel.loadGoals()
            }
        }
        .task {
            await viewModel.loadGoals()
        }
        .sheet(isPresented: $viewModel.showCelebration) {
            CelebrationView(message: viewModel.celebrationMessage ?? "")
        }
    }

    private var goalsList: some View {
        List {
            ForEach(viewModel.goals) { goal in
                NavigationLink(destination: GoalDetailView(goal: goal, viewModel: viewModel)) {
                    GoalRowView(goal: goal)
                }
            }
        }
        .listStyle(.plain)
    }
}

struct GoalRowView: View {
    let goal: SavingsGoal

    var body: some View {
        HStack(spacing: 12) {
            // Goal image or icon
            if let imageUrl = goal.imageUrl, let url = URL(string: imageUrl) {
                AsyncImage(url: url) { image in
                    image.resizable().aspectRatio(contentMode: .fill)
                } placeholder: {
                    Color.gray.opacity(0.3)
                }
                .frame(width: 60, height: 60)
                .cornerRadius(8)
            } else {
                Image(systemName: goal.category.iconName)
                    .font(.title)
                    .foregroundStyle(.secondary)
                    .frame(width: 60, height: 60)
                    .background(Color.gray.opacity(0.1))
                    .cornerRadius(8)
            }

            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Text(goal.name)
                        .font(.headline)
                    Spacer()
                    if goal.hasActiveChallenge {
                        Image(systemName: "flame.fill")
                            .foregroundStyle(.orange)
                    }
                    if goal.hasMatchingRule {
                        Image(systemName: "arrow.triangle.2.circlepath")
                            .foregroundStyle(.green)
                    }
                }

                Text("$\(goal.currentAmount, specifier: "%.2f") / $\(goal.targetAmount, specifier: "%.2f")")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)

                ProgressView(value: goal.progressPercentage / 100)
                    .tint(progressColor(for: goal.progressPercentage))

                if let daysRemaining = goal.daysRemaining {
                    Text("\(daysRemaining) days left")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding(.vertical, 4)
    }

    private func progressColor(for percentage: Double) -> Color {
        switch percentage {
        case 0..<25: return .red
        case 25..<50: return .orange
        case 50..<75: return .yellow
        case 75..<100: return .blue
        default: return .green
        }
    }
}

struct GoalDetailView: View {
    let goal: SavingsGoal
    @Bindable var viewModel: SavingsGoalViewModel
    @State private var showingContribute = false
    @State private var showingChallenge = false
    @State private var showingMatching = false

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Header with image and progress
                GoalHeaderView(goal: goal)

                // Milestones
                MilestonesView(milestones: goal.milestones)

                // Challenge section
                if let challenge = goal.activeChallenge {
                    ChallengeCardView(challenge: challenge)
                }

                // Matching rule section
                if let matching = goal.matchingRule {
                    MatchingRuleCardView(matching: matching)
                }

                // Contribution history
                ContributionHistoryView(contributions: viewModel.contributions)
            }
            .padding()
        }
        .navigationTitle(goal.name)
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Menu {
                    Button(action: { showingContribute = true }) {
                        Label("Add Money", systemImage: "plus.circle")
                    }

                    if !goal.hasMatchingRule {
                        Button(action: { showingMatching = true }) {
                            Label("Set Up Matching", systemImage: "arrow.triangle.2.circlepath")
                        }
                    }

                    if !goal.hasActiveChallenge {
                        Button(action: { showingChallenge = true }) {
                            Label("Create Challenge", systemImage: "flame")
                        }
                    }

                    if goal.status == .completed {
                        Button(action: { Task { await viewModel.markAsPurchased(goal.id) } }) {
                            Label("Mark as Purchased", systemImage: "checkmark.circle")
                        }
                    }
                } label: {
                    Image(systemName: "ellipsis.circle")
                }
            }
        }
        .sheet(isPresented: $showingContribute) {
            ContributeSheet(goal: goal, viewModel: viewModel)
        }
        .sheet(isPresented: $showingMatching) {
            SetupMatchingSheet(goal: goal, viewModel: viewModel)
        }
        .sheet(isPresented: $showingChallenge) {
            CreateChallengeSheet(goal: goal, viewModel: viewModel)
        }
        .task {
            await viewModel.loadGoalDetails(goal.id)
            await viewModel.loadContributions(goal.id)
        }
    }
}

struct GoalHeaderView: View {
    let goal: SavingsGoal

    var body: some View {
        VStack(spacing: 16) {
            // Image
            if let imageUrl = goal.imageUrl, let url = URL(string: imageUrl) {
                AsyncImage(url: url) { image in
                    image.resizable().aspectRatio(contentMode: .fit)
                } placeholder: {
                    ProgressView()
                }
                .frame(height: 200)
                .cornerRadius(12)
            }

            // Progress circle
            ZStack {
                Circle()
                    .stroke(Color.gray.opacity(0.3), lineWidth: 12)

                Circle()
                    .trim(from: 0, to: goal.progressPercentage / 100)
                    .stroke(
                        LinearGradient(colors: [.blue, .green], startPoint: .leading, endPoint: .trailing),
                        style: StrokeStyle(lineWidth: 12, lineCap: .round)
                    )
                    .rotationEffect(.degrees(-90))

                VStack {
                    Text("\(Int(goal.progressPercentage))%")
                        .font(.largeTitle)
                        .fontWeight(.bold)

                    Text("$\(goal.currentAmount, specifier: "%.2f")")
                        .font(.headline)
                        .foregroundStyle(.secondary)

                    Text("of $\(goal.targetAmount, specifier: "%.2f")")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
            .frame(width: 180, height: 180)
        }
    }
}

struct MilestonesView: View {
    let milestones: [Milestone]

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Milestones")
                .font(.headline)

            HStack(spacing: 0) {
                ForEach(milestones.sorted(by: { $0.percentComplete < $1.percentComplete })) { milestone in
                    VStack {
                        Circle()
                            .fill(milestone.isAchieved ? Color.green : Color.gray.opacity(0.3))
                            .frame(width: 24, height: 24)
                            .overlay(
                                milestone.isAchieved ?
                                    Image(systemName: "checkmark")
                                        .font(.caption)
                                        .foregroundColor(.white) : nil
                            )

                        Text("\(milestone.percentComplete)%")
                            .font(.caption)
                    }

                    if milestone.percentComplete < 100 {
                        Rectangle()
                            .fill(milestone.isAchieved ? Color.green : Color.gray.opacity(0.3))
                            .frame(height: 2)
                    }
                }
            }
        }
        .padding()
        .background(Color(.secondarySystemBackground))
        .cornerRadius(12)
    }
}

struct CelebrationView: View {
    let message: String
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        VStack(spacing: 24) {
            Spacer()

            Image(systemName: "star.fill")
                .font(.system(size: 80))
                .foregroundStyle(.yellow)

            Text(message)
                .font(.title)
                .fontWeight(.bold)
                .multilineTextAlignment(.center)

            Text("Keep up the great work!")
                .foregroundStyle(.secondary)

            Spacer()

            Button("Continue") {
                dismiss()
            }
            .buttonStyle(.borderedProminent)
        }
        .padding()
    }
}
```

---

## Testing Strategy

### Unit Tests - 45 tests

```csharp
public class SavingsGoalServiceTests
{
    // Goal CRUD (10 tests)
    [Fact]
    public async Task CreateGoal_WithAutoTransfer_SetsCorrectConfig() { }

    [Fact]
    public async Task CreateGoal_CreatesMilestones() { }

    // Contributions (12 tests)
    [Fact]
    public async Task Contribute_DeductsFromChildBalance() { }

    [Fact]
    public async Task Contribute_AppliesParentMatching() { }

    [Fact]
    public async Task Contribute_DoesNotExceedMatchingCap() { }

    [Fact]
    public async Task Contribute_AchievesMilestone() { }

    [Fact]
    public async Task Contribute_CompletesGoal() { }

    // Matching (8 tests)
    [Fact]
    public async Task CalculateMatch_RatioMatch_ReturnsCorrectAmount() { }

    [Fact]
    public async Task CalculateMatch_PercentageMatch_ReturnsCorrectAmount() { }

    [Fact]
    public async Task CalculateMatch_RespectsMaxCap() { }

    // Challenges (10 tests)
    [Fact]
    public async Task CompleteChallenge_AwardsBonusOnCompletion() { }

    [Fact]
    public async Task ExpiredChallenge_MarksAsFailed() { }

    // Auto-transfer (5 tests)
    [Fact]
    public async Task ProcessAutoTransfers_TransfersFixedAmount() { }

    [Fact]
    public async Task ProcessAutoTransfers_TransfersPercentage() { }
}
```

---

## Implementation Phases

### Phase 1: Database & Models (2 days)
- [ ] Create SavingsGoal model
- [ ] Create SavingsContribution model
- [ ] Create ParentMatchingRule model
- [ ] Create GoalMilestone model
- [ ] Create GoalChallenge model
- [ ] Add database migration

### Phase 2: Goal Service (3 days)
- [ ] Write ISavingsGoalService tests
- [ ] Implement goal CRUD
- [ ] Implement contribution logic with matching
- [ ] Implement milestone checking
- [ ] Implement goal completion

### Phase 3: Matching & Challenges (2 days)
- [ ] Implement matching rule CRUD
- [ ] Implement challenge CRUD
- [ ] Add challenge expiration job

### Phase 4: Auto-Transfer Integration (1 day)
- [ ] Integrate with AllowanceService
- [ ] Test auto-transfer on allowance deposit

### Phase 5: API Controllers (2 days)
- [ ] Write controller tests
- [ ] Implement SavingsGoalsController

### Phase 6: iOS Implementation (3 days)
- [ ] Create Swift models
- [ ] Create SavingsGoalViewModel
- [ ] Create UI views
- [ ] Add celebration animations

### Phase 7: React Implementation (2 days)
- [ ] Create TypeScript types
- [ ] Create SavingsGoals page
- [ ] Create goal detail components

---

## Success Criteria

- [ ] Goals track progress with visual indicators
- [ ] Parent matching applies automatically on contributions
- [ ] Milestones trigger at 25%, 50%, 75%, 100%
- [ ] Challenges award bonuses on completion
- [ ] Auto-transfer works on allowance deposit
- [ ] Notifications sent for milestones and completion
- [ ] >90% test coverage

---

This specification provides a complete goal-based savings system following TDD principles.
