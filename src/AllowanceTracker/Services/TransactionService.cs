using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Hubs;
using AllowanceTracker.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class TransactionService : ITransactionService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<FamilyHub>? _hubContext;

    public TransactionService(
        AllowanceContext context,
        ICurrentUserService currentUser,
        IHubContext<FamilyHub>? hubContext = null)
    {
        _context = context;
        _currentUser = currentUser;
        _hubContext = hubContext;
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

            // Broadcast to family members via SignalR
            if (_hubContext != null)
            {
                var childWithFamily = await _context.Children
                    .Include(c => c.Family)
                    .FirstAsync(c => c.Id == dto.ChildId);

                await _hubContext.Clients
                    .Group($"family-{childWithFamily.FamilyId}")
                    .SendAsync("TransactionCreated", dto.ChildId);
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
