namespace AllowanceTracker.Models;

/// <summary>
/// Defines how automatic transfers to savings goals are calculated
/// </summary>
public enum AutoTransferType
{
    /// <summary>
    /// No automatic transfers
    /// </summary>
    None = 0,

    /// <summary>
    /// Transfer a fixed dollar amount per allowance
    /// </summary>
    FixedAmount = 1,

    /// <summary>
    /// Transfer a percentage of the allowance
    /// </summary>
    Percentage = 2
}
