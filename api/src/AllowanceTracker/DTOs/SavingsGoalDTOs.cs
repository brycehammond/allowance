using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

// ============= Request DTOs =============

public record CreateSavingsGoalDto(
    Guid ChildId,
    string Name,
    string? Description,
    decimal TargetAmount,
    string? ImageUrl,
    string? ProductUrl,
    GoalCategory Category,
    DateTime? TargetDate,
    int Priority = 1,
    decimal AutoTransferAmount = 0,
    AutoTransferType AutoTransferType = AutoTransferType.None
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

// ============= Response DTOs =============

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
    DateTime? PurchasedAt,
    int Priority,
    decimal AutoTransferAmount,
    AutoTransferType AutoTransferType,
    bool HasMatchingRule,
    MatchingRuleSummaryDto? MatchingRule,
    bool HasActiveChallenge,
    ChallengeSummaryDto? ActiveChallenge,
    List<MilestoneDto> Milestones,
    DateTime CreatedAt
)
{
    public static SavingsGoalDto FromGoal(SavingsGoal goal)
    {
        var childName = goal.Child?.User?.FirstName ?? "Unknown";

        return new SavingsGoalDto(
            Id: goal.Id,
            ChildId: goal.ChildId,
            ChildName: childName,
            Name: goal.Name,
            Description: goal.Description,
            TargetAmount: goal.TargetAmount,
            CurrentAmount: goal.CurrentAmount,
            RemainingAmount: goal.RemainingAmount,
            ProgressPercentage: goal.ProgressPercentage,
            ImageUrl: goal.ImageUrl,
            ProductUrl: goal.ProductUrl,
            Category: goal.Category,
            CategoryName: goal.Category.ToString(),
            TargetDate: goal.TargetDate,
            DaysRemaining: goal.DaysRemaining,
            Status: goal.Status,
            StatusName: goal.Status.ToString(),
            CompletedAt: goal.CompletedAt,
            PurchasedAt: goal.PurchasedAt,
            Priority: goal.Priority,
            AutoTransferAmount: goal.AutoTransferAmount,
            AutoTransferType: goal.AutoTransferType,
            HasMatchingRule: goal.MatchingRule != null,
            MatchingRule: goal.MatchingRule != null ? MatchingRuleSummaryDto.FromRule(goal.MatchingRule) : null,
            HasActiveChallenge: goal.ActiveChallenge != null && goal.ActiveChallenge.Status == ChallengeStatus.Active,
            ActiveChallenge: goal.ActiveChallenge != null && goal.ActiveChallenge.Status == ChallengeStatus.Active
                ? ChallengeSummaryDto.FromChallenge(goal.ActiveChallenge, goal.CurrentAmount)
                : null,
            Milestones: goal.Milestones?.Select(MilestoneDto.FromMilestone).OrderBy(m => m.PercentComplete).ToList() ?? new List<MilestoneDto>(),
            CreatedAt: goal.CreatedAt
        );
    }
}

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
    bool CanAfford
)
{
    public static SavingsGoalSummaryDto FromGoal(SavingsGoal goal, decimal childBalance)
    {
        return new SavingsGoalSummaryDto(
            Id: goal.Id,
            Name: goal.Name,
            TargetAmount: goal.TargetAmount,
            CurrentAmount: goal.CurrentAmount,
            ProgressPercentage: goal.ProgressPercentage,
            ImageUrl: goal.ImageUrl,
            Category: goal.Category,
            TargetDate: goal.TargetDate,
            Status: goal.Status,
            CanAfford: childBalance >= goal.RemainingAmount
        );
    }
}

public record ContributionDto(
    Guid Id,
    decimal Amount,
    ContributionType Type,
    string TypeName,
    decimal GoalBalanceAfter,
    string? Description,
    DateTime CreatedAt,
    string? CreatedByName
)
{
    public static ContributionDto FromContribution(SavingsContribution contribution)
    {
        var createdByName = contribution.CreatedBy != null
            ? $"{contribution.CreatedBy.FirstName} {contribution.CreatedBy.LastName}"
            : null;

        return new ContributionDto(
            Id: contribution.Id,
            Amount: contribution.Amount,
            Type: contribution.Type,
            TypeName: contribution.Type.ToString(),
            GoalBalanceAfter: contribution.GoalBalanceAfter,
            Description: contribution.Description,
            CreatedAt: contribution.CreatedAt,
            CreatedByName: createdByName
        );
    }
}

public record MatchingRuleSummaryDto(
    Guid Id,
    MatchingType Type,
    string TypeDescription,
    decimal MatchRatio,
    decimal? MaxMatchAmount,
    decimal TotalMatchedAmount,
    decimal? RemainingMatchAmount,
    bool IsActive,
    DateTime? ExpiresAt
)
{
    public static MatchingRuleSummaryDto FromRule(ParentMatchingRule rule)
    {
        var typeDescription = rule.Type switch
        {
            MatchingType.RatioMatch => $"${1:F2} for every ${1 / rule.MatchRatio:F2} saved",
            MatchingType.PercentageMatch => $"Matches {rule.MatchRatio}% of each deposit",
            MatchingType.MilestoneBonus => "Bonus at milestones",
            _ => "Unknown matching type"
        };

        return new MatchingRuleSummaryDto(
            Id: rule.Id,
            Type: rule.Type,
            TypeDescription: typeDescription,
            MatchRatio: rule.MatchRatio,
            MaxMatchAmount: rule.MaxMatchAmount,
            TotalMatchedAmount: rule.TotalMatchedAmount,
            RemainingMatchAmount: rule.RemainingMatchAmount,
            IsActive: rule.IsActive,
            ExpiresAt: rule.ExpiresAt
        );
    }
}

public record MatchingRuleDto(
    Guid Id,
    Guid GoalId,
    string GoalName,
    MatchingType Type,
    string TypeDescription,
    decimal MatchRatio,
    decimal? MaxMatchAmount,
    decimal TotalMatchedAmount,
    decimal? RemainingMatchAmount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string CreatedByName
)
{
    public static MatchingRuleDto FromRule(ParentMatchingRule rule, string goalName)
    {
        var typeDescription = rule.Type switch
        {
            MatchingType.RatioMatch => $"${1:F2} for every ${1 / rule.MatchRatio:F2} saved",
            MatchingType.PercentageMatch => $"Matches {rule.MatchRatio}% of each deposit",
            MatchingType.MilestoneBonus => "Bonus at milestones",
            _ => "Unknown matching type"
        };

        var createdByName = rule.CreatedByParent != null
            ? $"{rule.CreatedByParent.FirstName} {rule.CreatedByParent.LastName}"
            : "Unknown";

        return new MatchingRuleDto(
            Id: rule.Id,
            GoalId: rule.GoalId,
            GoalName: goalName,
            Type: rule.Type,
            TypeDescription: typeDescription,
            MatchRatio: rule.MatchRatio,
            MaxMatchAmount: rule.MaxMatchAmount,
            TotalMatchedAmount: rule.TotalMatchedAmount,
            RemainingMatchAmount: rule.RemainingMatchAmount,
            IsActive: rule.IsActive,
            CreatedAt: rule.CreatedAt,
            ExpiresAt: rule.ExpiresAt,
            CreatedByName: createdByName
        );
    }
}

public record ChallengeSummaryDto(
    Guid Id,
    decimal TargetAmount,
    decimal CurrentProgress,
    double ProgressPercentage,
    DateTime EndDate,
    int DaysRemaining,
    decimal BonusAmount,
    ChallengeStatus Status
)
{
    public static ChallengeSummaryDto FromChallenge(GoalChallenge challenge, decimal currentGoalAmount)
    {
        var progressPercentage = challenge.TargetAmount > 0
            ? (double)(currentGoalAmount / challenge.TargetAmount * 100)
            : 0;

        return new ChallengeSummaryDto(
            Id: challenge.Id,
            TargetAmount: challenge.TargetAmount,
            CurrentProgress: currentGoalAmount,
            ProgressPercentage: Math.Min(100, progressPercentage),
            EndDate: challenge.EndDate,
            DaysRemaining: challenge.DaysRemaining,
            BonusAmount: challenge.BonusAmount,
            Status: challenge.Status
        );
    }
}

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
    string? Description,
    DateTime CreatedAt,
    string CreatedByName
)
{
    public static GoalChallengeDto FromChallenge(GoalChallenge challenge, string goalName, decimal currentGoalAmount)
    {
        var progressPercentage = challenge.TargetAmount > 0
            ? (double)(currentGoalAmount / challenge.TargetAmount * 100)
            : 0;

        var createdByName = challenge.CreatedByParent != null
            ? $"{challenge.CreatedByParent.FirstName} {challenge.CreatedByParent.LastName}"
            : "Unknown";

        return new GoalChallengeDto(
            Id: challenge.Id,
            GoalId: challenge.GoalId,
            GoalName: goalName,
            TargetAmount: challenge.TargetAmount,
            CurrentProgress: currentGoalAmount,
            ProgressPercentage: Math.Min(100, progressPercentage),
            StartDate: challenge.StartDate,
            EndDate: challenge.EndDate,
            DaysRemaining: challenge.DaysRemaining,
            BonusAmount: challenge.BonusAmount,
            Status: challenge.Status,
            StatusName: challenge.Status.ToString(),
            CompletedAt: challenge.CompletedAt,
            Description: challenge.Description,
            CreatedAt: challenge.CreatedAt,
            CreatedByName: createdByName
        );
    }
}

public record MilestoneDto(
    Guid Id,
    int PercentComplete,
    decimal TargetAmount,
    bool IsAchieved,
    DateTime? AchievedAt,
    string? CelebrationMessage,
    decimal? BonusAmount
)
{
    public static MilestoneDto FromMilestone(GoalMilestone milestone)
    {
        return new MilestoneDto(
            Id: milestone.Id,
            PercentComplete: milestone.PercentComplete,
            TargetAmount: milestone.TargetAmount,
            IsAchieved: milestone.IsAchieved,
            AchievedAt: milestone.AchievedAt,
            CelebrationMessage: milestone.CelebrationMessage,
            BonusAmount: milestone.BonusAmount
        );
    }
}

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
