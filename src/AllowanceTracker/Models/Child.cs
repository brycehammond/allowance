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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Family Family { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<WishListItem> WishListItems { get; set; } = new List<WishListItem>();
    public virtual ICollection<CategoryBudget> CategoryBudgets { get; set; } = new List<CategoryBudget>();
    public virtual ICollection<SavingsTransaction> SavingsTransactions { get; set; } = new List<SavingsTransaction>();
}
