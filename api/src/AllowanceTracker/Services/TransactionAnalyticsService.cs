using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

/// <summary>
/// Service for analyzing transaction data and generating insights
/// </summary>
public class TransactionAnalyticsService : ITransactionAnalyticsService
{
    private readonly AllowanceContext _context;

    public TransactionAnalyticsService(AllowanceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get balance history for a child over a specified number of days
    /// Returns daily balance snapshots for charting
    /// </summary>
    public async Task<List<BalancePoint>> GetBalanceHistoryAsync(Guid childId, int days = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var endDate = DateTime.UtcNow.Date;

        var transactions = await _context.Transactions
            .Where(t => t.ChildId == childId && t.CreatedAt >= startDate)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        if (!transactions.Any())
            return new List<BalancePoint>();

        var balancePoints = new List<BalancePoint>();
        var lastBalance = 0m;
        var hasStartedTracking = false;

        var totalDays = (int)(endDate - startDate).TotalDays + 1; // +1 to include end date

        for (int i = 0; i < totalDays; i++)
        {
            var currentDate = startDate.AddDays(i);

            // Find all transactions for this day
            var dayTransactions = transactions
                .Where(t => t.CreatedAt.Date == currentDate)
                .ToList();

            if (dayTransactions.Any())
            {
                // Use the last transaction's balance for this day
                var lastTrans = dayTransactions.Last();
                lastBalance = lastTrans.BalanceAfter;
                balancePoints.Add(new BalancePoint(currentDate, lastBalance, lastTrans.Description));
                hasStartedTracking = true;
            }
            else if (hasStartedTracking)
            {
                // No transactions this day, use previous balance to fill gap
                balancePoints.Add(new BalancePoint(currentDate, lastBalance, null));
            }
        }

        return balancePoints;
    }

    /// <summary>
    /// Get income vs spending summary for a date range
    /// Calculates total income (credits), spending (debits), and savings rate
    /// </summary>
    public async Task<IncomeSpendingSummary> GetIncomeVsSpendingAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Transactions.Where(t => t.ChildId == childId);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        var transactions = await query.ToListAsync();

        var income = transactions
            .Where(t => t.Type == TransactionType.Credit)
            .Sum(t => t.Amount);

        var spending = transactions
            .Where(t => t.Type == TransactionType.Debit)
            .Sum(t => t.Amount);

        var incomeCount = transactions.Count(t => t.Type == TransactionType.Credit);
        var spendingCount = transactions.Count(t => t.Type == TransactionType.Debit);

        var netSavings = income - spending;
        var savingsRate = income > 0 ? (netSavings / income) * 100 : 0m;

        return new IncomeSpendingSummary(
            income,
            spending,
            netSavings,
            incomeCount,
            spendingCount,
            savingsRate);
    }

    /// <summary>
    /// Get spending trend data for analysis
    /// Calculates trend direction and percentage change
    /// </summary>
    public async Task<TrendData> GetSpendingTrendAsync(Guid childId, TimePeriod period)
    {
        var days = (int)period;
        var startDate = days == 0 ? DateTime.MinValue : DateTime.UtcNow.Date.AddDays(-days);

        var transactions = await _context.Transactions
            .Where(t => t.ChildId == childId
                && t.Type == TransactionType.Debit
                && t.CreatedAt >= startDate)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        if (!transactions.Any())
        {
            return new TrendData(
                new List<DataPoint>(),
                TrendDirection.Stable,
                0m,
                "No spending data available");
        }

        // Create data points from transactions
        var dataPoints = transactions
            .Select(t => new DataPoint(t.CreatedAt, t.Amount))
            .ToList();

        // Calculate trend
        if (transactions.Count < 2)
        {
            return new TrendData(
                dataPoints,
                TrendDirection.Stable,
                0m,
                "Insufficient data for trend analysis");
        }

        // Simple trend: compare first half vs second half of period
        var midPoint = transactions.Count / 2;
        var firstHalf = transactions.Take(midPoint).ToList();
        var secondHalf = transactions.Skip(midPoint).ToList();

        var firstAvg = firstHalf.Any() ? firstHalf.Average(t => t.Amount) : 0m;
        var secondAvg = secondHalf.Any() ? secondHalf.Average(t => t.Amount) : 0m;

        var changePercent = firstAvg > 0
            ? ((secondAvg - firstAvg) / firstAvg) * 100
            : 0m;

        var direction = Math.Abs(changePercent) < 0.01m
            ? TrendDirection.Stable
            : changePercent > 0
                ? TrendDirection.Up
                : TrendDirection.Down;

        var description = direction switch
        {
            TrendDirection.Up => $"Spending increased by {Math.Abs(changePercent):F1}%",
            TrendDirection.Down => $"Spending decreased by {Math.Abs(changePercent):F1}%",
            _ => "Spending is stable"
        };

        return new TrendData(dataPoints, direction, changePercent, description);
    }

    /// <summary>
    /// Calculate savings rate (money saved vs money earned)
    /// Returns percentage: ((Income - Spending) / Income) * 100
    /// </summary>
    public async Task<decimal> GetSavingsRateAsync(Guid childId, TimePeriod period)
    {
        var days = (int)period;
        var startDate = days == 0 ? (DateTime?)null : DateTime.UtcNow.Date.AddDays(-days);

        var summary = await GetIncomeVsSpendingAsync(childId, startDate);
        return summary.SavingsRate;
    }

    /// <summary>
    /// Get monthly comparison data for the last N months
    /// Groups transactions by month and calculates income, spending, and balance
    /// </summary>
    public async Task<List<MonthlyComparison>> GetMonthlyComparisonAsync(Guid childId, int months = 6)
    {
        var currentDate = DateTime.UtcNow.Date;
        var startDate = currentDate.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1); // First day of the month

        var transactions = await _context.Transactions
            .Where(t => t.ChildId == childId && t.CreatedAt >= startDate)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var monthlyData = new List<MonthlyComparison>();

        // Get initial balance from last transaction before the period
        var initialBalance = await _context.Transactions
            .Where(t => t.ChildId == childId && t.CreatedAt < startDate)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.BalanceAfter)
            .FirstOrDefaultAsync();

        var runningBalance = initialBalance;

        // Process each month from oldest to newest
        for (int i = 0; i < months; i++)
        {
            var monthStart = startDate.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);

            var monthTransactions = transactions
                .Where(t => t.CreatedAt >= monthStart && t.CreatedAt < monthEnd)
                .ToList();

            var income = monthTransactions
                .Where(t => t.Type == TransactionType.Credit)
                .Sum(t => t.Amount);

            var spending = monthTransactions
                .Where(t => t.Type == TransactionType.Debit)
                .Sum(t => t.Amount);

            var netSavings = income - spending;

            // Use the actual ending balance from last transaction of month if available
            var lastTransaction = monthTransactions.LastOrDefault();
            if (lastTransaction != null)
            {
                runningBalance = lastTransaction.BalanceAfter;
            }
            else
            {
                runningBalance += netSavings;
            }

            monthlyData.Add(new MonthlyComparison(
                monthStart.Year,
                monthStart.Month,
                monthStart.ToString("MMMM"),
                income,
                spending,
                netSavings,
                runningBalance));
        }

        return monthlyData.OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).ToList();
    }

    /// <summary>
    /// Get transaction count by day for heatmap visualization
    /// Returns activity data for each day in the specified range
    /// </summary>
    public async Task<Dictionary<DateTime, TransactionDayData>> GetTransactionHeatmapDataAsync(
        Guid childId,
        int days = 365)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        var transactions = await _context.Transactions
            .Where(t => t.ChildId == childId && t.CreatedAt >= startDate)
            .ToListAsync();

        var heatmapData = transactions
            .GroupBy(t => t.CreatedAt.Date)
            .ToDictionary(
                g => g.Key,
                g => new TransactionDayData(
                    g.Count(),
                    g.Sum(t => t.Amount),
                    g.Sum(t => t.Type == TransactionType.Credit ? t.Amount : -t.Amount)));

        return heatmapData;
    }

    /// <summary>
    /// Get spending breakdown by category
    /// Categories are inferred from transaction description patterns
    /// </summary>
    public async Task<List<CategoryBreakdown>> GetSpendingBreakdownAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Where(t => t.ChildId == childId && t.Type == TransactionType.Debit);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        var transactions = await query.ToListAsync();

        if (!transactions.Any())
            return new List<CategoryBreakdown>();

        // Infer category from description (simple keyword matching)
        var categorized = transactions
            .GroupBy(t => InferCategory(t.Description))
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToList();

        var totalSpending = categorized.Sum(c => c.Amount);

        return categorized
            .Select(c => new CategoryBreakdown(
                c.Category,
                c.Amount,
                c.Count,
                totalSpending > 0 ? (c.Amount / totalSpending) * 100 : 0))
            .OrderByDescending(c => c.Amount)
            .ToList();
    }

    #region Helper Methods

    private static string InferCategory(string description)
    {
        var desc = description.ToLowerInvariant();

        // Simple keyword-based categorization
        if (desc.Contains("toy") || desc.Contains("game") || desc.Contains("lego"))
            return "Toys & Games";

        if (desc.Contains("snack") || desc.Contains("candy") || desc.Contains("food") || desc.Contains("drink"))
            return "Food & Snacks";

        if (desc.Contains("book") || desc.Contains("school"))
            return "Education";

        if (desc.Contains("save") || desc.Contains("savings"))
            return "Savings";

        if (desc.Contains("gift") || desc.Contains("present"))
            return "Gifts";

        if (desc.Contains("clothes") || desc.Contains("shirt") || desc.Contains("shoes"))
            return "Clothing";

        if (desc.Contains("movie") || desc.Contains("entertainment"))
            return "Entertainment";

        return "Other";
    }

    #endregion
}
