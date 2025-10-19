namespace AllowanceTracker.DTOs;

/// <summary>
/// Monthly comparison data for income vs spending analysis
/// </summary>
public record MonthlyComparison(
    int Year,
    int Month,
    string MonthName,
    decimal Income,
    decimal Spending,
    decimal NetSavings,
    decimal EndingBalance);
