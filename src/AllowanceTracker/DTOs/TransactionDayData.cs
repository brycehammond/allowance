namespace AllowanceTracker.DTOs;

/// <summary>
/// Transaction data for a single day (for heatmap visualization)
/// </summary>
public record TransactionDayData(
    int TransactionCount,
    decimal TotalAmount,
    decimal NetChange);
