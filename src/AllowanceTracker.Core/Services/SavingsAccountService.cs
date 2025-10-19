using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class SavingsAccountService : ISavingsAccountService
{
    private readonly AllowanceContext _context;

    public SavingsAccountService(AllowanceContext context)
    {
        _context = context;
    }

    // Configuration Methods

    public async Task EnableSavingsAccountAsync(Guid childId, SavingsTransferType transferType, decimal amount)
    {
        if (!ValidateSavingsConfig(transferType, amount))
        {
            throw new InvalidOperationException("Invalid savings configuration. Percentage must be between 0 and 100.");
        }

        var child = await _context.Children.FindAsync(childId);
        if (child == null)
        {
            throw new InvalidOperationException($"Child with ID {childId} not found.");
        }

        child.SavingsAccountEnabled = true;
        child.SavingsTransferType = transferType;

        if (transferType == SavingsTransferType.FixedAmount)
        {
            child.SavingsTransferAmount = amount;
            child.SavingsTransferPercentage = 0;
        }
        else if (transferType == SavingsTransferType.Percentage)
        {
            child.SavingsTransferPercentage = (int)amount;
            child.SavingsTransferAmount = 0;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DisableSavingsAccountAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId);
        if (child == null)
        {
            throw new InvalidOperationException($"Child with ID {childId} not found.");
        }

        child.SavingsAccountEnabled = false;
        child.SavingsTransferType = SavingsTransferType.None;
        // Keep SavingsBalance intact - don't reset it

        await _context.SaveChangesAsync();
    }

    public async Task UpdateSavingsConfigAsync(Guid childId, SavingsTransferType transferType, decimal amount)
    {
        if (!ValidateSavingsConfig(transferType, amount))
        {
            throw new InvalidOperationException("Invalid savings configuration. Percentage must be between 0 and 100.");
        }

        var child = await _context.Children.FindAsync(childId);
        if (child == null)
        {
            throw new InvalidOperationException($"Child with ID {childId} not found.");
        }

        child.SavingsTransferType = transferType;

        if (transferType == SavingsTransferType.FixedAmount)
        {
            child.SavingsTransferAmount = amount;
            child.SavingsTransferPercentage = 0;
        }
        else if (transferType == SavingsTransferType.Percentage)
        {
            child.SavingsTransferPercentage = (int)amount;
            child.SavingsTransferAmount = 0;
        }

        await _context.SaveChangesAsync();
    }

    // Manual Transaction Methods

    public async Task<SavingsTransaction> DepositToSavingsAsync(Guid childId, decimal amount, string description, Guid userId)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Deposit amount must be positive.");
        }

        var child = await _context.Children.FindAsync(childId);
        if (child == null)
        {
            throw new InvalidOperationException($"Child with ID {childId} not found.");
        }

        if (child.CurrentBalance < amount)
        {
            throw new InvalidOperationException($"Insufficient balance. Current balance: {child.CurrentBalance:C}, Requested: {amount:C}");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Deduct from current balance
            child.CurrentBalance -= amount;

            // Add to savings balance
            child.SavingsBalance += amount;

            // Create transaction record
            var savingsTransaction = new SavingsTransaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = amount,
                Type = SavingsTransactionType.Deposit,
                Description = description,
                BalanceAfter = child.SavingsBalance,
                IsAutomatic = false,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(savingsTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return savingsTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SavingsTransaction> WithdrawFromSavingsAsync(Guid childId, decimal amount, string description, Guid userId)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Withdrawal amount must be positive.");
        }

        var child = await _context.Children.FindAsync(childId);
        if (child == null)
        {
            throw new InvalidOperationException($"Child with ID {childId} not found.");
        }

        if (child.SavingsBalance < amount)
        {
            throw new InvalidOperationException($"Insufficient savings balance. Current balance: {child.SavingsBalance:C}, Requested: {amount:C}");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Deduct from savings balance
            child.SavingsBalance -= amount;

            // Add to current balance
            child.CurrentBalance += amount;

            // Create transaction record
            var savingsTransaction = new SavingsTransaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = amount,
                Type = SavingsTransactionType.Withdrawal,
                Description = description,
                BalanceAfter = child.SavingsBalance,
                IsAutomatic = false,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(savingsTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return savingsTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Automatic Transfer Method

    public async Task ProcessAutomaticTransferAsync(Guid childId, Guid allowanceTransactionId, decimal allowanceAmount)
    {
        var child = await _context.Children.FindAsync(childId);
        if (child == null)
        {
            throw new InvalidOperationException($"Child with ID {childId} not found.");
        }

        // Check if savings account is enabled
        if (!child.SavingsAccountEnabled || child.SavingsTransferType == SavingsTransferType.None)
        {
            return; // Do nothing if not enabled
        }

        // Calculate transfer amount
        var transferAmount = CalculateTransferAmount(
            allowanceAmount,
            child.SavingsTransferType,
            child.SavingsTransferType == SavingsTransferType.FixedAmount
                ? child.SavingsTransferAmount
                : child.SavingsTransferPercentage);

        // Check if child has sufficient balance
        if (child.CurrentBalance < transferAmount)
        {
            return; // Skip transfer if insufficient balance
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Deduct from current balance
            child.CurrentBalance -= transferAmount;

            // Add to savings balance
            child.SavingsBalance += transferAmount;

            // Create automatic transfer record
            var savingsTransaction = new SavingsTransaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = transferAmount,
                Type = SavingsTransactionType.AutoTransfer,
                Description = $"Automatic transfer from allowance ({child.SavingsTransferType})",
                BalanceAfter = child.SavingsBalance,
                IsAutomatic = true,
                SourceAllowanceTransactionId = allowanceTransactionId,
                CreatedById = child.UserId, // System created, but associated with child
                CreatedAt = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(savingsTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Query Methods

    public async Task<decimal> GetSavingsBalanceAsync(Guid childId)
    {
        var child = await _context.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            throw new InvalidOperationException($"Child with ID {childId} not found.");
        }

        return child.SavingsBalance;
    }

    public async Task<List<SavingsTransaction>> GetSavingsHistoryAsync(Guid childId, int limit = 50)
    {
        return await _context.SavingsTransactions
            .AsNoTracking()
            .Where(t => t.ChildId == childId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Include(t => t.CreatedBy)
            .ToListAsync();
    }

    public async Task<SavingsAccountSummary> GetSummaryAsync(Guid childId)
    {
        var child = await _context.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            throw new InvalidOperationException($"Child with ID {childId} not found.");
        }

        var transactions = await _context.SavingsTransactions
            .AsNoTracking()
            .Where(t => t.ChildId == childId)
            .ToListAsync();

        var totalDeposited = transactions
            .Where(t => t.Type == SavingsTransactionType.Deposit || t.Type == SavingsTransactionType.AutoTransfer)
            .Sum(t => t.Amount);

        var totalWithdrawn = transactions
            .Where(t => t.Type == SavingsTransactionType.Withdrawal)
            .Sum(t => t.Amount);

        var lastTransactionDate = transactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault()?.CreatedAt;

        var configDescription = child.SavingsTransferType switch
        {
            SavingsTransferType.FixedAmount => $"Saves {child.SavingsTransferAmount:C} per allowance",
            SavingsTransferType.Percentage => $"Saves {child.SavingsTransferPercentage}% per allowance",
            _ => "No automatic transfer configured"
        };

        return new SavingsAccountSummary(
            ChildId: childId,
            IsEnabled: child.SavingsAccountEnabled,
            CurrentBalance: child.SavingsBalance,
            TransferType: child.SavingsTransferType,
            TransferAmount: child.SavingsTransferAmount,
            TransferPercentage: child.SavingsTransferPercentage,
            TotalTransactions: transactions.Count,
            TotalDeposited: totalDeposited,
            TotalWithdrawn: totalWithdrawn,
            LastTransactionDate: lastTransactionDate,
            ConfigDescription: configDescription
        );
    }

    // Validation & Calculation Methods

    public decimal CalculateTransferAmount(decimal allowanceAmount, SavingsTransferType type, decimal configValue)
    {
        return type switch
        {
            SavingsTransferType.FixedAmount => configValue,
            SavingsTransferType.Percentage => Math.Round(allowanceAmount * (configValue / 100m), 2),
            _ => 0m
        };
    }

    public bool ValidateSavingsConfig(SavingsTransferType type, decimal amount)
    {
        if (type == SavingsTransferType.Percentage)
        {
            return amount >= 0 && amount <= 100;
        }

        if (type == SavingsTransferType.FixedAmount)
        {
            return amount >= 0;
        }

        return true;
    }
}
