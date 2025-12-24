using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// Tracks all deposits and withdrawals in savings account
/// Provides complete audit trail
/// </summary>
public class SavingsTransaction : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    /// <summary>
    /// Amount transferred (positive for deposits, negative for withdrawals)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Type of savings transaction
    /// </summary>
    public SavingsTransactionType Type { get; set; }

    /// <summary>
    /// Description of the transaction
    /// Example: "Auto-transfer from allowance", "Manual deposit", "Withdrawal for purchase"
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Balance in savings account after this transaction
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Was this an automatic transfer? (vs manual deposit/withdrawal)
    /// </summary>
    public bool IsAutomatic { get; set; } = false;

    /// <summary>
    /// Reference to the allowance transaction that triggered this (if auto-transfer)
    /// </summary>
    public Guid? SourceAllowanceTransactionId { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
