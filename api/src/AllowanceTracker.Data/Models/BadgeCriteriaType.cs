namespace AllowanceTracker.Models;

/// <summary>
/// Types of criteria used to determine when a badge is unlocked
/// </summary>
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
