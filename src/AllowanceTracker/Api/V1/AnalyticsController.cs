using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ITransactionAnalyticsService _analyticsService;

    public AnalyticsController(ITransactionAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Get balance history for a child over specified number of days
    /// </summary>
    [HttpGet("children/{childId}/balance-history")]
    public async Task<ActionResult<List<BalancePoint>>> GetBalanceHistory(Guid childId, [FromQuery] int days = 30)
    {
        var balanceHistory = await _analyticsService.GetBalanceHistoryAsync(childId, days);
        return Ok(balanceHistory);
    }

    /// <summary>
    /// Get income vs spending summary for a child
    /// </summary>
    [HttpGet("children/{childId}/income-spending")]
    public async Task<ActionResult<IncomeSpendingSummary>> GetIncomeVsSpending(
        Guid childId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var summary = await _analyticsService.GetIncomeVsSpendingAsync(childId, startDate, endDate);
        return Ok(summary);
    }

    /// <summary>
    /// Get spending trend for a child over specified time period
    /// </summary>
    [HttpGet("children/{childId}/spending-trend")]
    public async Task<ActionResult<TrendData>> GetSpendingTrend(
        Guid childId,
        [FromQuery] TimePeriod period = TimePeriod.Week)
    {
        var trendData = await _analyticsService.GetSpendingTrendAsync(childId, period);
        return Ok(trendData);
    }

    /// <summary>
    /// Get savings rate for a child over specified time period
    /// </summary>
    [HttpGet("children/{childId}/savings-rate")]
    public async Task<ActionResult<object>> GetSavingsRate(
        Guid childId,
        [FromQuery] TimePeriod period = TimePeriod.Month)
    {
        var savingsRate = await _analyticsService.GetSavingsRateAsync(childId, period);
        return Ok(new { savingsRate });
    }

    /// <summary>
    /// Get monthly comparison data for a child
    /// </summary>
    [HttpGet("children/{childId}/monthly-comparison")]
    public async Task<ActionResult<List<MonthlyComparison>>> GetMonthlyComparison(
        Guid childId,
        [FromQuery] int months = 6)
    {
        var monthlyData = await _analyticsService.GetMonthlyComparisonAsync(childId, months);
        return Ok(monthlyData);
    }

    /// <summary>
    /// Get transaction heatmap data for a child
    /// </summary>
    [HttpGet("children/{childId}/heatmap")]
    public async Task<ActionResult<Dictionary<DateTime, TransactionDayData>>> GetTransactionHeatmap(
        Guid childId,
        [FromQuery] int days = 365)
    {
        var heatmapData = await _analyticsService.GetTransactionHeatmapDataAsync(childId, days);
        return Ok(heatmapData);
    }

    /// <summary>
    /// Get spending breakdown by category for a child
    /// </summary>
    [HttpGet("children/{childId}/spending-breakdown")]
    public async Task<ActionResult<List<CategoryBreakdown>>> GetSpendingBreakdown(
        Guid childId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var breakdown = await _analyticsService.GetSpendingBreakdownAsync(childId, startDate, endDate);
        return Ok(breakdown);
    }
}
