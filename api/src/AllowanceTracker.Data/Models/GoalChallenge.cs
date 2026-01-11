using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class GoalChallenge : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }
    public virtual SavingsGoal Goal { get; set; } = null!;

    public Guid CreatedByParentId { get; set; }
    public virtual ApplicationUser CreatedByParent { get; set; } = null!;

    // Challenge parameters
    /// <summary>
    /// The amount that must be saved to complete the challenge
    /// </summary>
    public decimal TargetAmount { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// The bonus amount awarded upon successful completion
    /// </summary>
    public decimal BonusAmount { get; set; }

    // Status
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Active;
    public DateTime? CompletedAt { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed properties
    public int DaysRemaining => Math.Max(0, (int)(EndDate - DateTime.UtcNow).TotalDays);
    public bool IsExpired => DateTime.UtcNow > EndDate && Status == ChallengeStatus.Active;
}
