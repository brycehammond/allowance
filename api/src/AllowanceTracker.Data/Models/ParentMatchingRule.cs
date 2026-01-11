using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class ParentMatchingRule : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }
    public virtual SavingsGoal Goal { get; set; } = null!;

    public Guid CreatedByParentId { get; set; }
    public virtual ApplicationUser CreatedByParent { get; set; } = null!;

    // Matching configuration
    public MatchingType Type { get; set; }

    /// <summary>
    /// For RatioMatch: The ratio of parent contribution per child contribution.
    /// E.g., 0.5 means parent adds $1 for every $2 the child saves.
    /// For PercentageMatch: The percentage of each deposit to match.
    /// E.g., 50 means parent matches 50% of each deposit.
    /// </summary>
    public decimal MatchRatio { get; set; }

    /// <summary>
    /// Maximum total amount the parent will match. Null means no limit.
    /// </summary>
    public decimal? MaxMatchAmount { get; set; }

    // Tracking
    public decimal TotalMatchedAmount { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Computed property
    public decimal? RemainingMatchAmount => MaxMatchAmount.HasValue
        ? Math.Max(0, MaxMatchAmount.Value - TotalMatchedAmount)
        : null;
}
