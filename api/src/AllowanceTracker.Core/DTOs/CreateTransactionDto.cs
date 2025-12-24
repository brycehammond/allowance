using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Request model for creating a new transaction
/// </summary>
/// <param name="ChildId">ID of the child this transaction belongs to</param>
/// <param name="Amount">Transaction amount (must be positive; type determines if it's added or subtracted)</param>
/// <param name="Type">Transaction type (Credit = adds money, Debit = subtracts money, Allowance = weekly allowance)</param>
/// <param name="Category">Category for the transaction (Allowance, Chore, Gift, Purchase, Savings, Other)</param>
/// <param name="Description">Description or note for the transaction</param>
/// <param name="Notes">Optional additional notes or details about the transaction</param>
public record CreateTransactionDto(
    Guid ChildId,
    decimal Amount,
    TransactionType Type,
    TransactionCategory Category,
    string Description,
    string? Notes = null);
