using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

// Category spending summary
public record CategorySpendingDto(
    TransactionCategory Category,
    string CategoryName,
    decimal TotalAmount,
    int TransactionCount,
    decimal Percentage);

// Budget status with current spending
public record CategoryBudgetStatusDto(
    TransactionCategory Category,
    string CategoryName,
    decimal BudgetLimit,
    decimal CurrentSpending,
    decimal Remaining,
    int PercentUsed,
    BudgetStatus Status,
    BudgetPeriod Period);

// Budget status enum
public enum BudgetStatus
{
    Safe,        // < 80% used
    Warning,     // 80-99% used
    AtLimit,     // 100% used
    OverBudget   // > 100% used
}

// Budget check result
public record BudgetCheckResult(
    bool Allowed,
    string Message,
    decimal CurrentSpending,
    decimal BudgetLimit,
    decimal RemainingAfter);

// Set budget DTO
public record SetBudgetDto(
    Guid ChildId,
    TransactionCategory Category,
    decimal Limit,
    BudgetPeriod Period,
    int AlertThresholdPercent = 80,
    bool EnforceLimit = false);
