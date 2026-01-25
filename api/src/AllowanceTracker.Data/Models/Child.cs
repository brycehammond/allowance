using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class Child : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public decimal WeeklyAllowance { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0;
    public DateTime? LastAllowanceDate { get; set; }

    /// <summary>
    /// Optional preferred day of the week for allowance payment (0=Sunday, 6=Saturday).
    /// If null, uses rolling 7-day window from LastAllowanceDate.
    /// </summary>
    public DayOfWeek? AllowanceDay { get; set; }

    /// <summary>
    /// Indicates if the allowance is currently paused
    /// </summary>
    public bool AllowancePaused { get; set; } = false;

    /// <summary>
    /// Optional reason for pausing the allowance
    /// </summary>
    public string? AllowancePausedReason { get; set; }

    // Savings Account Properties
    /// <summary>
    /// Is savings account feature enabled for this child?
    /// </summary>
    public bool SavingsAccountEnabled { get; set; } = false;

    /// <summary>
    /// Current balance in savings account
    /// </summary>
    public decimal SavingsBalance { get; set; } = 0;

    /// <summary>
    /// Transfer type: None, FixedAmount, or Percentage
    /// </summary>
    public SavingsTransferType SavingsTransferType { get; set; } = SavingsTransferType.None;

    /// <summary>
    /// Fixed dollar amount to transfer (if SavingsTransferType == FixedAmount)
    /// Example: 5.00 means transfer $5 per allowance
    /// </summary>
    public decimal SavingsTransferAmount { get; set; } = 0;

    /// <summary>
    /// Percentage of allowance to transfer (if SavingsTransferType == Percentage)
    /// Example: 20 means transfer 20% of each allowance
    /// Range: 0-100
    /// </summary>
    public int SavingsTransferPercentage { get; set; } = 0;

    /// <summary>
    /// Controls whether the child can see their savings balance.
    /// When false, the savings balance is hidden from child users at the API level.
    /// Parents can always see the savings balance regardless of this setting.
    /// </summary>
    public bool SavingsBalanceVisibleToChild { get; set; } = true;

    /// <summary>
    /// When true, allows the child's spending balance to go negative (into debt).
    /// When false (default), transactions that would result in a negative balance are rejected.
    /// </summary>
    public bool AllowDebt { get; set; } = false;

    // Achievement System Properties
    /// <summary>
    /// Total points earned from all badges
    /// </summary>
    public int TotalPoints { get; set; } = 0;

    /// <summary>
    /// Points available to spend on rewards
    /// </summary>
    public int AvailablePoints { get; set; } = 0;

    /// <summary>
    /// URL of the currently equipped avatar reward
    /// </summary>
    public string? EquippedAvatarUrl { get; set; }

    /// <summary>
    /// Identifier of the currently equipped theme reward
    /// </summary>
    public string? EquippedTheme { get; set; }

    /// <summary>
    /// Currently equipped title reward
    /// </summary>
    public string? EquippedTitle { get; set; }

    /// <summary>
    /// Current saving streak in weeks (consecutive weeks with savings)
    /// </summary>
    public int SavingStreak { get; set; } = 0;

    /// <summary>
    /// Date of last savings activity for streak tracking
    /// </summary>
    public DateTime? LastSavingDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Family Family { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<CategoryBudget> CategoryBudgets { get; set; } = new List<CategoryBudget>();
    public virtual ICollection<SavingsTransaction> SavingsTransactions { get; set; } = new List<SavingsTransaction>();
    public virtual ICollection<ChoreTask> Tasks { get; set; } = new List<ChoreTask>();
    public virtual ICollection<TaskCompletion> TaskCompletions { get; set; } = new List<TaskCompletion>();
    public virtual ICollection<AllowanceAdjustment> AllowanceAdjustments { get; set; } = new List<AllowanceAdjustment>();
    public virtual ICollection<ChildBadge> Badges { get; set; } = new List<ChildBadge>();
    public virtual ICollection<BadgeProgress> BadgeProgress { get; set; } = new List<BadgeProgress>();
    public virtual ICollection<ChildReward> Rewards { get; set; } = new List<ChildReward>();
    public virtual ICollection<SavingsGoal> SavingsGoals { get; set; } = new List<SavingsGoal>();
    public virtual ICollection<SavingsContribution> SavingsContributions { get; set; } = new List<SavingsContribution>();
    public virtual ICollection<GiftLink> GiftLinks { get; set; } = new List<GiftLink>();
    public virtual ICollection<Gift> Gifts { get; set; } = new List<Gift>();
    public virtual ICollection<ThankYouNote> ThankYouNotes { get; set; } = new List<ThankYouNote>();
}
