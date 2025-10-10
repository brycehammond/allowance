using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface ICategoryBudgetService
{
    /// <summary>
    /// Create or update a budget for a category
    /// </summary>
    Task<CategoryBudget> SetBudgetAsync(SetBudgetDto dto, Guid currentUserId);

    /// <summary>
    /// Get budget for a specific category
    /// </summary>
    Task<CategoryBudget?> GetBudgetAsync(Guid childId, TransactionCategory category);

    /// <summary>
    /// Get all budgets for a child
    /// </summary>
    Task<List<CategoryBudget>> GetAllBudgetsAsync(Guid childId);

    /// <summary>
    /// Delete a budget
    /// </summary>
    Task DeleteBudgetAsync(Guid budgetId, Guid currentUserId);

    /// <summary>
    /// Check if parent can manage budgets for child
    /// </summary>
    Task<bool> CanManageBudgetsAsync(Guid childId, Guid userId);
}
