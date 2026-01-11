namespace AllowanceTracker.Models;

public class GoalMilestone
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }
    public virtual SavingsGoal Goal { get; set; } = null!;

    /// <summary>
    /// The percentage complete for this milestone (e.g., 25, 50, 75, 100)
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// The target amount to reach this milestone (calculated from goal target)
    /// </summary>
    public decimal TargetAmount { get; set; }

    public bool IsAchieved { get; set; } = false;
    public DateTime? AchievedAt { get; set; }

    /// <summary>
    /// Optional celebration message to show when milestone is reached
    /// </summary>
    public string? CelebrationMessage { get; set; }

    /// <summary>
    /// Optional bonus amount awarded when this milestone is reached
    /// </summary>
    public decimal? BonusAmount { get; set; }
}
