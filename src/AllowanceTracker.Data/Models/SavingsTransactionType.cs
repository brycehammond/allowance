namespace AllowanceTracker.Models;

/// <summary>
/// Types of savings account transactions
/// </summary>
public enum SavingsTransactionType
{
    /// <summary>
    /// Manual deposit to savings
    /// </summary>
    Deposit = 1,

    /// <summary>
    /// Withdrawal from savings
    /// </summary>
    Withdrawal = 2,

    /// <summary>
    /// Automatic transfer from allowance
    /// </summary>
    AutoTransfer = 3,

    /// <summary>
    /// Interest earned (future enhancement)
    /// </summary>
    Interest = 4
}
