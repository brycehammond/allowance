using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class TransactionService : ITransactionService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryService? _categoryService;
    private readonly INotificationService? _notificationService;
    private readonly IAchievementService? _achievementService;

    public TransactionService(
        AllowanceContext context,
        ICurrentUserService currentUser,
        ICategoryService? categoryService = null,
        INotificationService? notificationService = null,
        IAchievementService? achievementService = null)
    {
        _context = context;
        _currentUser = currentUser;
        _categoryService = categoryService;
        _notificationService = notificationService;
        _achievementService = achievementService;
    }

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
    {
        // Use database transaction for atomicity
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var child = await _context.Children.FindAsync(dto.ChildId)
                ?? throw new InvalidOperationException("Child not found");

            // Handle insufficient spending balance for debits
            if (dto.Type == TransactionType.Debit && child.CurrentBalance < dto.Amount)
            {
                var shortfall = dto.Amount - child.CurrentBalance;
                var totalAvailable = child.CurrentBalance + child.SavingsBalance;

                if (dto.DrawFromSavings)
                {
                    // User explicitly chose to draw from savings
                    if (child.SavingsBalance >= shortfall)
                    {
                        // Savings covers the shortfall
                        child.SavingsBalance -= shortfall;
                        child.CurrentBalance += shortfall;
                    }
                    else if (child.AllowDebt)
                    {
                        // Drain all savings, then allow going into debt
                        child.CurrentBalance += child.SavingsBalance;
                        child.SavingsBalance = 0;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Insufficient funds. Total available (spending + savings): {totalAvailable:C}");
                    }
                }
                else
                {
                    // User didn't explicitly choose to draw from savings
                    if (child.AllowDebt)
                    {
                        // AllowDebt is enabled - proceed (will go negative)
                    }
                    else
                    {
                        throw new InvalidOperationException("Insufficient funds");
                    }
                }
            }

            // Check budget limits for debits (if CategoryService is available)
            if (dto.Type == TransactionType.Debit && _categoryService != null)
            {
                var budgetCheck = await _categoryService.CheckBudgetAsync(dto.ChildId, dto.Category, dto.Amount);
                if (!budgetCheck.Allowed)
                {
                    throw new InvalidOperationException(budgetCheck.Message);
                }
            }

            // Update balance
            if (dto.Type == TransactionType.Credit)
                child.CurrentBalance += dto.Amount;
            else
                child.CurrentBalance -= dto.Amount;

            // Create immutable transaction record with balance snapshot
            var transaction = new Transaction
            {
                ChildId = dto.ChildId,
                Amount = dto.Amount,
                Type = dto.Type,
                Category = dto.Category,
                Description = dto.Description,
                Notes = dto.Notes,
                BalanceAfter = child.CurrentBalance,
                CreatedById = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Send notification to the child about the transaction
            if (_notificationService != null)
            {
                try
                {
                    var typeText = dto.Type == TransactionType.Credit ? "received" : "spent";
                    var title = dto.Type == TransactionType.Credit ? "Money Added" : "Purchase Recorded";
                    var body = $"You {typeText} {dto.Amount:C} - {dto.Description}";

                    await _notificationService.SendNotificationAsync(
                        child.UserId,
                        NotificationType.TransactionCreated,
                        title,
                        body,
                        data: new { transactionId = transaction.Id, amount = dto.Amount, type = dto.Type.ToString(), category = dto.Category.ToString() },
                        relatedEntityId: transaction.Id,
                        relatedEntityType: "Transaction");
                }
                catch
                {
                    // Don't fail the transaction if notification fails
                }
            }

            // Check for achievement badges
            if (_achievementService != null)
            {
                try
                {
                    await _achievementService.CheckAndUnlockBadgesAsync(
                        dto.ChildId,
                        BadgeTrigger.TransactionCreated,
                        new { TransactionId = transaction.Id, Amount = dto.Amount, Type = dto.Type, Category = dto.Category });

                    // Also check balance-related badges
                    await _achievementService.CheckAndUnlockBadgesAsync(
                        dto.ChildId,
                        BadgeTrigger.BalanceChanged,
                        new { NewBalance = child.CurrentBalance });
                }
                catch
                {
                    // Don't fail the transaction if badge check fails
                }
            }

            return transaction;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Transaction>> GetChildTransactionsAsync(Guid childId, int limit = 20)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Include(t => t.CreatedBy)
            .Where(t => t.ChildId == childId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<decimal> GetCurrentBalanceAsync(Guid childId)
    {
        var child = await _context.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId)
            ?? throw new InvalidOperationException("Child not found");

        return child.CurrentBalance;
    }
}
