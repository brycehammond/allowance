using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

public record TransactionDto(
    Guid Id,
    Guid ChildId,
    decimal Amount,
    string Type,
    string Category,
    string Description,
    decimal BalanceAfter,
    DateTime CreatedAt,
    string CreatedByName,
    string? Notes)
{
    public static TransactionDto FromTransaction(Transaction transaction)
    {
        return new TransactionDto(
            transaction.Id,
            transaction.ChildId,
            transaction.Amount,
            transaction.Type.ToString(),
            transaction.Category.ToString(),
            transaction.Description,
            transaction.BalanceAfter,
            transaction.CreatedAt,
            $"{transaction.CreatedBy.FirstName} {transaction.CreatedBy.LastName}",
            transaction.Notes);
    }
}
