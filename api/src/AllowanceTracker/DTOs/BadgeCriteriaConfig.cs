using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Configuration for badge unlock criteria
/// </summary>
public class BadgeCriteriaConfig
{
    /// <summary>
    /// For SingleAction: the action type to check (e.g., "first_savings_deposit")
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// For CountThreshold: the target count to reach
    /// </summary>
    public int? CountTarget { get; set; }

    /// <summary>
    /// For AmountThreshold: the target amount to reach
    /// </summary>
    public decimal? AmountTarget { get; set; }

    /// <summary>
    /// For StreakCount: the number of consecutive periods required
    /// </summary>
    public int? StreakTarget { get; set; }

    /// <summary>
    /// For PercentageTarget: the percentage to achieve (0-100)
    /// </summary>
    public int? PercentageTarget { get; set; }

    /// <summary>
    /// For GoalCompletion: number of goals to complete
    /// </summary>
    public int? GoalTarget { get; set; }

    /// <summary>
    /// For TimeBasedAction: specific timing requirements
    /// </summary>
    public string? TimeCondition { get; set; }

    /// <summary>
    /// Optional: filter by transaction type
    /// </summary>
    public TransactionType? TransactionType { get; set; }

    /// <summary>
    /// Optional: field to measure (e.g., "current_balance", "total_saved", "transaction_count")
    /// </summary>
    public string? MeasureField { get; set; }

    /// <summary>
    /// For Compound: list of sub-criteria that must all be met
    /// </summary>
    public List<BadgeCriteriaConfig>? SubCriteria { get; set; }

    /// <summary>
    /// Triggers that should evaluate this badge
    /// </summary>
    public List<BadgeTrigger>? Triggers { get; set; }
}
