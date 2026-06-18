using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AllowanceTracker.Services;

public class AllowanceService : IAllowanceService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITransactionService _transactionService;
    private readonly ISavingsAccountService? _savingsAccountService;
    private readonly ILogger<AllowanceService>? _logger;

    public AllowanceService(
        AllowanceContext context,
        ICurrentUserService currentUser,
        ITransactionService transactionService,
        ISavingsAccountService? savingsAccountService = null,
        ILogger<AllowanceService>? logger = null)
    {
        _context = context;
        _currentUser = currentUser;
        _transactionService = transactionService;
        _savingsAccountService = savingsAccountService;
        _logger = logger;
    }

    public async Task PayWeeklyAllowanceAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        if (child.WeeklyAllowance <= 0)
            throw new InvalidOperationException("Child has no weekly allowance configured");

        // Don't pay a paused allowance.
        if (child.AllowancePaused)
            throw new InvalidOperationException($"Allowance is currently paused{(string.IsNullOrEmpty(child.AllowancePausedReason) ? "" : $": {child.AllowancePausedReason}")}");

        // Determine eligibility and the date to record as the payment anchor. A fixed AllowanceDay
        // no longer hard-blocks payment on other days: if a scheduled payday has passed unpaid
        // (e.g. the daily timer missed a run), the child is caught up now and re-anchored to that
        // scheduled day so the weekly cadence realigns to the intended weekday instead of drifting
        // or skipping the week. Dates are compared by calendar day, not timestamp, to avoid
        // sub-second jitter skipping a week (the "every other week" bug).
        var (eligible, anchor) = EvaluateAllowanceEligibility(child, DateTime.UtcNow);
        if (!eligible)
        {
            // A brand-new child with a fixed day waits for that day before the first payment.
            if (child.AllowanceDay.HasValue && !child.LastAllowanceDate.HasValue)
                throw new InvalidOperationException($"Today is {DateTime.UtcNow.DayOfWeek}, but this child's allowance is scheduled for {child.AllowanceDay.Value}. This is not the scheduled allowance day.");

            throw new InvalidOperationException("Allowance already paid this week");
        }

        // Create transaction for allowance payment
        var dto = new CreateTransactionDto(
            childId,
            child.WeeklyAllowance,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            "Weekly Allowance");

        var transaction = await _transactionService.CreateTransactionAsync(dto);

        // Process automatic savings transfer if enabled
        if (_savingsAccountService != null)
        {
            try
            {
                await _savingsAccountService.ProcessAutomaticTransferAsync(
                    childId, transaction.Id, child.WeeklyAllowance);

                _logger?.LogInformation(
                    "Processed automatic savings transfer for child {ChildId}",
                    childId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Failed to process automatic savings transfer for child {ChildId}. Allowance was paid successfully.",
                    childId);
                // Don't throw - allowance payment succeeded, savings transfer is optional
            }
        }

        // Update last allowance date (re-anchored to the scheduled payday when a fixed day is set).
        child.LastAllowanceDate = anchor;
        await _context.SaveChangesAsync();

        _logger?.LogInformation(
            "Paid weekly allowance of {Amount} to child {ChildId}",
            child.WeeklyAllowance,
            childId);
    }

    public async Task ProcessAllPendingAllowancesAsync()
    {
        var children = await _context.Children
            .Where(c => c.WeeklyAllowance > 0 && !c.AllowancePaused)
            .ToListAsync();

        var processedCount = 0;
        var errorCount = 0;

        foreach (var child in children)
        {
            try
            {
                // Eligible when a scheduled payday is due (fixed day, including catch-up of a missed
                // run) or the rolling 7-calendar-day window has elapsed. See EvaluateAllowanceEligibility.
                var (eligible, _) = EvaluateAllowanceEligibility(child, DateTime.UtcNow);
                if (eligible)
                {
                    await PayWeeklyAllowanceAsync(child.Id);
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger?.LogError(ex,
                    "Failed to process allowance for child {ChildId}",
                    child.Id);
                // Continue processing other children even if one fails
            }
        }

        _logger?.LogInformation(
            "Processed {ProcessedCount} allowances with {ErrorCount} errors",
            processedCount,
            errorCount);
    }

    /// <summary>
    /// Determines whether a child is due an allowance and the date to record as the payment anchor.
    /// For a fixed <see cref="Child.AllowanceDay"/>, the child is due whenever a scheduled payday
    /// (the most recent occurrence of that weekday on or before today) has passed unpaid -- paying
    /// on the scheduled day normally and catching up a missed run on a later day -- and the anchor
    /// is that scheduled payday so the cadence re-aligns to the intended weekday. A first-ever
    /// payment still waits for the scheduled day. Without a fixed day, a rolling 7-calendar-day
    /// window is used. Comparisons are by calendar date, not timestamp.
    /// </summary>
    internal static (bool Eligible, DateTime Anchor) EvaluateAllowanceEligibility(Child child, DateTime nowUtc)
    {
        var today = nowUtc.Date;

        if (child.AllowanceDay is DayOfWeek day)
        {
            if (!child.LastAllowanceDate.HasValue)
            {
                // First payment must land on the scheduled day.
                return (today.DayOfWeek == day, today);
            }

            var scheduledPayday = MostRecentOccurrence(today, day);
            return (child.LastAllowanceDate.Value.Date < scheduledPayday, scheduledPayday);
        }

        // Rolling window: pay once at least 7 calendar days have elapsed.
        var daysSince = child.LastAllowanceDate.HasValue
            ? (today - child.LastAllowanceDate.Value.Date).Days
            : int.MaxValue;
        return (daysSince >= 7, nowUtc);
    }

    /// <summary>Returns the most recent date on or before <paramref name="today"/> whose weekday is <paramref name="day"/>.</summary>
    private static DateTime MostRecentOccurrence(DateTime today, DayOfWeek day)
    {
        var diff = ((int)today.DayOfWeek - (int)day + 7) % 7;
        return today.Date.AddDays(-diff);
    }
}
