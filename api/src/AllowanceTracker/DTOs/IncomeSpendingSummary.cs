namespace AllowanceTracker.DTOs;

/// <summary>
/// Summary of income vs spending for a period
/// </summary>
public record IncomeSpendingSummary(
    decimal TotalIncome,
    decimal TotalSpending,
    decimal NetSavings,
    int IncomeTransactionCount,
    int SpendingTransactionCount,
    decimal SavingsRate);
