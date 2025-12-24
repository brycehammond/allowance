using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface ITransactionService
{
    Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto);
    Task<List<Transaction>> GetChildTransactionsAsync(Guid childId, int limit = 20);
    Task<decimal> GetCurrentBalanceAsync(Guid childId);
}
