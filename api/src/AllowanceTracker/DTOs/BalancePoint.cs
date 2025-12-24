namespace AllowanceTracker.DTOs;

/// <summary>
/// Represents a point in time with a balance value for charting balance history
/// </summary>
public record BalancePoint(
    DateTime Date,
    decimal Balance,
    string? TransactionDescription);
