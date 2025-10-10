using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

public record CreateTransactionDto(
    Guid ChildId,
    decimal Amount,
    TransactionType Type,
    TransactionCategory Category,
    string Description);
