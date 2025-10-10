using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class TransactionService : ITransactionService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;

    public TransactionService(AllowanceContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
    {
        // Use database transaction for atomicity
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var child = await _context.Children.FindAsync(dto.ChildId)
                ?? throw new InvalidOperationException("Child not found");

            // Validate balance for debits
            if (dto.Type == TransactionType.Debit && child.CurrentBalance < dto.Amount)
                throw new InvalidOperationException("Insufficient funds");

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
                Description = dto.Description,
                BalanceAfter = child.CurrentBalance,
                CreatedById = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

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
