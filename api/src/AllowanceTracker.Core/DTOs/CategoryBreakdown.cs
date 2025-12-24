namespace AllowanceTracker.DTOs;

/// <summary>
/// Spending breakdown by category (inferred from transaction descriptions)
/// </summary>
public record CategoryBreakdown(
    string Category,
    decimal Amount,
    int TransactionCount,
    decimal Percentage);
