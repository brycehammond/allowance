using AllowanceTracker.DTOs;

namespace AllowanceTracker.Services;

/// <summary>
/// Service for analyzing transaction data and generating insights
/// </summary>
public interface ITransactionAnalyticsService
{
    /// <summary>
    /// Get balance history for a child over a specified number of days
    /// Returns daily balance snapshots for charting
    /// </summary>
    /// <param name="childId">The ID of the child</param>
    /// <param name="days">Number of days to include (default 30)</param>
    /// <returns>List of balance points with dates and transaction info</returns>
    Task<List<BalancePoint>> GetBalanceHistoryAsync(Guid childId, int days = 30);

    /// <summary>
    /// Get income vs spending summary for a date range
    /// Calculates total income (credits), spending (debits), and savings rate
    /// </summary>
    /// <param name="childId">The ID of the child</param>
    /// <param name="startDate">Start date for analysis (null = all time)</param>
    /// <param name="endDate">End date for analysis (null = today)</param>
    /// <returns>Summary with totals and savings rate</returns>
    Task<IncomeSpendingSummary> GetIncomeVsSpendingAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Get spending trend data for analysis
    /// Calculates trend direction and percentage change
    /// </summary>
    /// <param name="childId">The ID of the child</param>
    /// <param name="period">Time period for trend analysis</param>
    /// <returns>Trend data with direction and change percentage</returns>
    Task<TrendData> GetSpendingTrendAsync(Guid childId, TimePeriod period);

    /// <summary>
    /// Calculate savings rate (money saved vs money earned)
    /// Returns percentage: ((Income - Spending) / Income) * 100
    /// </summary>
    /// <param name="childId">The ID of the child</param>
    /// <param name="period">Time period for calculation</param>
    /// <returns>Savings rate as a percentage (0-100)</returns>
    Task<decimal> GetSavingsRateAsync(Guid childId, TimePeriod period);

    /// <summary>
    /// Get monthly comparison data for the last N months
    /// Groups transactions by month and calculates income, spending, and balance
    /// </summary>
    /// <param name="childId">The ID of the child</param>
    /// <param name="months">Number of months to include (default 6)</param>
    /// <returns>List of monthly summaries in reverse chronological order</returns>
    Task<List<MonthlyComparison>> GetMonthlyComparisonAsync(Guid childId, int months = 6);

    /// <summary>
    /// Get transaction count by day for heatmap visualization
    /// Returns activity data for each day in the specified range
    /// </summary>
    /// <param name="childId">The ID of the child</param>
    /// <param name="days">Number of days to include (default 365)</param>
    /// <returns>Dictionary mapping dates to transaction day data</returns>
    Task<Dictionary<DateTime, TransactionDayData>> GetTransactionHeatmapDataAsync(
        Guid childId,
        int days = 365);

    /// <summary>
    /// Get spending breakdown by category
    /// Categories are inferred from transaction description patterns
    /// </summary>
    /// <param name="childId">The ID of the child</param>
    /// <param name="startDate">Start date for analysis (null = all time)</param>
    /// <param name="endDate">End date for analysis (null = today)</param>
    /// <returns>List of category breakdowns with amounts and percentages</returns>
    Task<List<CategoryBreakdown>> GetSpendingBreakdownAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null);
}
