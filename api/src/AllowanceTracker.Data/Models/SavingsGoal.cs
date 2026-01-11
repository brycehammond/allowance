using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class SavingsGoal : IHasCreatedAt
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

    // Priority (for ordering and auto-transfer processing)
    public int Priority { get; set; } = 1;

    // Auto-transfer settings
    public decimal AutoTransferAmount { get; set; } = 0;
    public AutoTransferType AutoTransferType { get; set; } = AutoTransferType.None;

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<SavingsContribution> Contributions { get; set; } = new List<SavingsContribution>();
    public virtual ICollection<GoalMilestone> Milestones { get; set; } = new List<GoalMilestone>();
    public virtual ParentMatchingRule? MatchingRule { get; set; }
    public virtual GoalChallenge? ActiveChallenge { get; set; }

    // Computed properties
    public decimal RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);
    public double ProgressPercentage => TargetAmount > 0 ? (double)(CurrentAmount / TargetAmount * 100) : 0;
    public int? DaysRemaining => TargetDate.HasValue ? (int?)(TargetDate.Value - DateTime.UtcNow).TotalDays : null;
}
