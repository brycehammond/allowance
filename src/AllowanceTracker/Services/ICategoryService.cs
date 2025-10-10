using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface ICategoryService
{
    /// <summary>
    /// Get spending breakdown by category for a date range
    /// </summary>
    Task<List<CategorySpendingDto>> GetCategorySpendingAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Get current spending vs budget for all categories
    /// </summary>
    Task<List<CategoryBudgetStatusDto>> GetBudgetStatusAsync(
        Guid childId,
        BudgetPeriod period);

    /// <summary>
    /// Check if a transaction would exceed budget
    /// </summary>
    Task<BudgetCheckResult> CheckBudgetAsync(
        Guid childId,
        TransactionCategory category,
        decimal amount);

    /// <summary>
    /// Get suggested category for a transaction based on description
    /// </summary>
    TransactionCategory SuggestCategory(string description, TransactionType type);

    /// <summary>
    /// Get all categories appropriate for transaction type
    /// </summary>
    List<TransactionCategory> GetCategoriesForType(TransactionType type);
}
