using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.DTOs.Allowances;
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
    private readonly ISavingsGoalService? _savingsGoalService;
    private readonly INotificationService? _notificationService;
    private readonly IAchievementService? _achievementService;
    private readonly ILogger<AllowanceService> _logger;

    public AllowanceService(
        AllowanceContext context,
        ICurrentUserService currentUser,
        ITransactionService transactionService,
        ILogger<AllowanceService> logger,
        ISavingsAccountService? savingsAccountService = null,
        ISavingsGoalService? savingsGoalService = null,
        INotificationService? notificationService = null,
        IAchievementService? achievementService = null)
    {
        _context = context;
        _currentUser = currentUser;
        _transactionService = transactionService;
        _logger = logger;
        _savingsAccountService = savingsAccountService;
        _savingsGoalService = savingsGoalService;
        _notificationService = notificationService;
        _achievementService = achievementService;
    }

    public async Task PayWeeklyAllowanceAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        if (child.WeeklyAllowance <= 0)
            throw new InvalidOperationException("Child has no weekly allowance configured");

        // Check if allowance is paused
        if (child.AllowancePaused)
            throw new InvalidOperationException($"Allowance is currently paused{(string.IsNullOrEmpty(child.AllowancePausedReason) ? "" : $": {child.AllowancePausedReason}")}");

        // Determine eligibility and the date to record as the payment anchor. A fixed AllowanceDay
        // no longer hard-blocks payment on other days: if a scheduled payday has passed unpaid
        // (e.g. the daily timer missed a run), the child is caught up now and re-anchored to that
        // scheduled day so the weekly cadence realigns to the intended weekday instead of drifting
        // or skipping the week entirely. Dates are compared by calendar day, not timestamp.
        var (eligible, anchor) = EvaluateAllowanceEligibility(child, DateTime.UtcNow);
        if (!eligible)
        {
            // A brand-new child with a fixed day waits for that day before the first payment.
            if (child.AllowanceDay.HasValue && !child.LastAllowanceDate.HasValue)
                throw new InvalidOperationException($"Today is {DateTime.UtcNow.DayOfWeek}, but this child's allowance is scheduled for {child.AllowanceDay.Value}. This is not the scheduled allowance day.");

            _logger.LogDebug(
                "Child {ChildId} not eligible: last payment on {LastPaymentDate}",
                childId, child.LastAllowanceDate?.Date);
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

                _logger.LogInformation(
                    "Processed automatic savings transfer for child {ChildId}",
                    childId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process automatic savings transfer for child {ChildId}. Allowance was paid successfully.",
                    childId);
                // Don't throw - allowance payment succeeded, savings transfer is optional
            }
        }

        // Process automatic transfers to savings goals
        if (_savingsGoalService != null)
        {
            try
            {
                await _savingsGoalService.ProcessAutoTransfersAsync(childId, child.WeeklyAllowance);

                _logger.LogInformation(
                    "Processed savings goal auto-transfers for child {ChildId}",
                    childId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process savings goal auto-transfers for child {ChildId}. Allowance was paid successfully.",
                    childId);
                // Don't throw - allowance payment succeeded, auto-transfers are optional
            }
        }

        // Update last allowance date (re-anchored to the scheduled payday when a fixed day is set).
        child.LastAllowanceDate = anchor;
        await _context.SaveChangesAsync();

        // Send notification to the child
        if (_notificationService != null)
        {
            try
            {
                await _notificationService.SendNotificationAsync(
                    child.UserId,
                    NotificationType.AllowanceDeposit,
                    "Allowance Received!",
                    $"Your weekly allowance of {child.WeeklyAllowance:C} has been deposited.",
                    data: new { childId = childId, amount = child.WeeklyAllowance });
            }
            catch
            {
                // Don't fail the allowance payment if notification fails
            }
        }

        // Check for badge unlocks after allowance payment
        if (_achievementService != null)
        {
            try
            {
                await _achievementService.CheckAndUnlockBadgesAsync(
                    childId,
                    BadgeTrigger.AllowanceReceived,
                    new { Amount = child.WeeklyAllowance });

                // Also check balance-related badges
                await _achievementService.CheckAndUnlockBadgesAsync(
                    childId,
                    BadgeTrigger.BalanceChanged,
                    new { NewBalance = child.CurrentBalance });
            }
            catch
            {
                // Don't fail the allowance payment if badge check fails
            }
        }

        _logger.LogInformation(
            "Paid weekly allowance of {Amount} to child {ChildId}",
            child.WeeklyAllowance,
            childId);
    }

    public async Task ProcessAllPendingAllowancesAsync()
    {
        _logger.LogInformation("Starting allowance processing run at {Time}", DateTime.UtcNow);

        var children = await _context.Children
            .Include(c => c.User)
            .Where(c => c.WeeklyAllowance > 0 && !c.AllowancePaused)
            .ToListAsync();

        _logger.LogInformation("Found {Count} children eligible for allowance processing", children.Count);

        var processedCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        foreach (var child in children)
        {
            var childName = child.User != null ? $"{child.User.FirstName} {child.User.LastName}" : child.Id.ToString();

            try
            {
                // Eligible when a scheduled payday is due (fixed day, including catch-up of a missed
                // run) or the rolling 7-calendar-day window has elapsed. See EvaluateAllowanceEligibility.
                var (eligible, _) = EvaluateAllowanceEligibility(child, DateTime.UtcNow);
                if (!eligible)
                {
                    _logger.LogDebug(
                        "Skipping {ChildName} ({ChildId}): not due yet (last payment on {LastPaymentDate}, AllowanceDay {AllowanceDay})",
                        childName, child.Id, child.LastAllowanceDate?.Date, child.AllowanceDay);
                    skippedCount++;
                    continue;
                }

                _logger.LogInformation(
                    "Processing allowance for {ChildName} ({ChildId}): ${Amount}",
                    childName, child.Id, child.WeeklyAllowance);

                await PayWeeklyAllowanceAsync(child.Id);
                processedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex,
                    "Failed to process allowance for {ChildName} ({ChildId})",
                    childName, child.Id);
                // Continue processing other children even if one fails
            }
        }

        _logger.LogInformation(
            "Allowance processing complete: {ProcessedCount} paid, {SkippedCount} skipped, {ErrorCount} errors",
            processedCount, skippedCount, errorCount);
    }

    public async Task PauseAllowanceAsync(Guid childId, string? reason)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        child.AllowancePaused = true;
        child.AllowancePausedReason = reason;

        // Create adjustment history record
        var adjustment = new AllowanceAdjustment
        {
            ChildId = childId,
            AdjustmentType = AllowanceAdjustmentType.Paused,
            Reason = reason,
            AdjustedById = _currentUser.UserId
        };
        _context.AllowanceAdjustments.Add(adjustment);

        await _context.SaveChangesAsync();

        // Send notification to the child
        if (_notificationService != null)
        {
            try
            {
                var reasonText = string.IsNullOrEmpty(reason) ? "" : $" Reason: {reason}";
                await _notificationService.SendNotificationAsync(
                    child.UserId,
                    NotificationType.AllowancePaused,
                    "Allowance Paused",
                    $"Your allowance has been paused.{reasonText}",
                    data: new { childId = childId, reason = reason });
            }
            catch
            {
                // Don't fail the pause operation if notification fails
            }
        }

        _logger.LogInformation(
            "Paused allowance for child {ChildId}. Reason: {Reason}",
            childId,
            reason ?? "Not specified");
    }

    public async Task ResumeAllowanceAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        child.AllowancePaused = false;
        child.AllowancePausedReason = null;

        // Create adjustment history record
        var adjustment = new AllowanceAdjustment
        {
            ChildId = childId,
            AdjustmentType = AllowanceAdjustmentType.Resumed,
            AdjustedById = _currentUser.UserId
        };
        _context.AllowanceAdjustments.Add(adjustment);

        await _context.SaveChangesAsync();

        // Send notification to the child
        if (_notificationService != null)
        {
            try
            {
                await _notificationService.SendNotificationAsync(
                    child.UserId,
                    NotificationType.AllowanceResumed,
                    "Allowance Resumed",
                    "Your allowance has been resumed. You will receive your next payment on schedule.",
                    data: new { childId = childId });
            }
            catch
            {
                // Don't fail the resume operation if notification fails
            }
        }

        _logger.LogInformation(
            "Resumed allowance for child {ChildId}",
            childId);
    }

    public async Task AdjustAllowanceAmountAsync(Guid childId, decimal newAmount, string? reason)
    {
        if (newAmount < 0)
            throw new ArgumentException("Allowance amount cannot be negative", nameof(newAmount));

        var child = await _context.Children.FindAsync(childId)
            ?? throw new InvalidOperationException("Child not found");

        var oldAmount = child.WeeklyAllowance;
        child.WeeklyAllowance = newAmount;

        // Create adjustment history record
        var adjustment = new AllowanceAdjustment
        {
            ChildId = childId,
            AdjustmentType = AllowanceAdjustmentType.AmountChanged,
            OldAmount = oldAmount,
            NewAmount = newAmount,
            Reason = reason,
            AdjustedById = _currentUser.UserId
        };
        _context.AllowanceAdjustments.Add(adjustment);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Adjusted allowance for child {ChildId} from {OldAmount} to {NewAmount}. Reason: {Reason}",
            childId,
            oldAmount,
            newAmount,
            reason ?? "Not specified");
    }

    public async Task<List<AllowanceAdjustmentDto>> GetAllowanceAdjustmentHistoryAsync(Guid childId)
    {
        var adjustments = await (
            from a in _context.AllowanceAdjustments
            join u in _context.Users on a.AdjustedById equals u.Id into userJoin
            from u in userJoin.DefaultIfEmpty()
            where a.ChildId == childId
            orderby a.CreatedAt
            select new AllowanceAdjustmentDto(
                a.Id,
                a.ChildId,
                a.AdjustmentType,
                a.OldAmount,
                a.NewAmount,
                a.Reason,
                a.AdjustedById,
                u != null ? $"{u.FirstName} {u.LastName}" : "Unknown",
                a.CreatedAt)
        ).ToListAsync();

        return adjustments;
    }

    /// <summary>
    /// Determines whether a child is due an allowance and the date to record as the payment anchor.
    /// </summary>
    /// <remarks>
    /// For a fixed <see cref="Child.AllowanceDay"/>, the child is due whenever a scheduled payday
    /// (the most recent occurrence of that weekday on or before today) has passed that has not yet
    /// been paid. This pays on the scheduled day in the normal case and catches up a missed run on
    /// a later day, and the anchor is set to that scheduled payday so the cadence re-aligns to the
    /// intended weekday rather than drifting. A first-ever payment still waits for the scheduled day.
    /// Without a fixed day, a rolling 7-calendar-day window is used. All comparisons are by calendar
    /// date, not timestamp, to avoid sub-second jitter skipping a week (the "every other week" bug).
    /// </remarks>
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
