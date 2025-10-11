namespace AllowanceTracker.Models;

/// <summary>
/// Defines how savings transfers are calculated
/// </summary>
public enum SavingsTransferType
{
    /// <summary>
    /// No automatic transfer configured
    /// </summary>
    None = 0,

    /// <summary>
    /// Transfer a fixed dollar amount
    /// Example: Always transfer $5.00 per allowance
    /// </summary>
    FixedAmount = 1,

    /// <summary>
    /// Transfer a percentage of the allowance
    /// Example: Transfer 20% of each allowance
    /// </summary>
    Percentage = 2
}
