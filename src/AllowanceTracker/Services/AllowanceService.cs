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
    private readonly ILogger<AllowanceService>? _logger;

    public AllowanceService(
        AllowanceContext context,
        ICurrentUserService currentUser,
        ITransactionService transactionService,
        ILogger<AllowanceService>? logger = null)
    {
        _context = context;
        _currentUser = currentUser;
        _transactionService = transactionService;
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

        // Create transaction for allowance payment
        var dto = new CreateTransactionDto(
            childId,
            child.WeeklyAllowance,
            TransactionType.Credit,
            $"Weekly Allowance - {DateTime.UtcNow:yyyy-MM-dd}");

        await _transactionService.CreateTransactionAsync(dto);

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

        foreach (var child in children)
        {
            try
            {
                // Check if child is eligible for allowance payment
                if (!child.LastAllowanceDate.HasValue ||
                    (DateTime.UtcNow - child.LastAllowanceDate.Value).TotalDays >= 7)
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
