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

        // Check if allowance was already paid this week
        if (child.LastAllowanceDate.HasValue)
        {
            var daysSinceLastPayment = (DateTime.UtcNow - child.LastAllowanceDate.Value).TotalDays;
            if (daysSinceLastPayment < 7)
                throw new InvalidOperationException("Allowance already paid this week");
        }

        // If AllowanceDay is set, verify today matches the scheduled day
        if (child.AllowanceDay.HasValue)
        {
            var today = DateTime.UtcNow.DayOfWeek;
            if (today != child.AllowanceDay.Value)
                throw new InvalidOperationException($"Today is {today}, but this child's allowance is scheduled for {child.AllowanceDay.Value}. This is not the scheduled allowance day.");
        }

        // Create transaction for allowance payment
        var dto = new CreateTransactionDto(
            childId,
            child.WeeklyAllowance,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            $"Weekly Allowance - {DateTime.UtcNow:yyyy-MM-dd}");

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

        // Update last allowance date
        child.LastAllowanceDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger?.LogInformation(
            "Paid weekly allowance of {Amount} to child {ChildId}",
            child.WeeklyAllowance,
            childId);
    }

    public async Task ProcessAllPendingAllowancesAsync()
    {
        var children = await _context.Children
            .Where(c => c.WeeklyAllowance > 0)
            .ToListAsync();

        var processedCount = 0;
        var errorCount = 0;
        var today = DateTime.UtcNow.DayOfWeek;

        foreach (var child in children)
        {
            try
            {
                // Check if child is eligible for allowance payment
                var timingEligible = !child.LastAllowanceDate.HasValue ||
                    (DateTime.UtcNow - child.LastAllowanceDate.Value).TotalDays >= 7;

                // If AllowanceDay is set, also check if today matches
                var dayEligible = !child.AllowanceDay.HasValue || child.AllowanceDay.Value == today;

                if (timingEligible && dayEligible)
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
}
