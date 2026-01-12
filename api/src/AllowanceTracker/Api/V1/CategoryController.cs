using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Get categories for a specific transaction type
    /// </summary>
    /// <param name="type">Transaction type (Credit or Debit)</param>
    /// <returns>List of categories for the specified type</returns>
    [HttpGet]
    public IActionResult GetCategories([FromQuery] TransactionType type)
    {
        var categories = _categoryService.GetCategoriesForType(type);
        return Ok(categories);
    }

    /// <summary>
    /// Get all available categories
    /// </summary>
    /// <returns>List of all transaction categories</returns>
    [HttpGet("all")]
    public IActionResult GetAllCategories()
    {
        var categories = _categoryService.GetAllCategories();
        return Ok(categories);
    }

    /// <summary>
    /// Get display name for a category
    /// </summary>
    /// <param name="category">Category enum value</param>
    /// <returns>Formatted display name</returns>
    [HttpGet("{category:int}/display-name")]
    public IActionResult GetCategoryDisplayName(TransactionCategory category)
    {
        var displayName = _categoryService.GetCategoryDisplayName(category);
        return Ok(displayName);
    }

    /// <summary>
    /// Get spending breakdown by category for a child
    /// </summary>
    /// <param name="childId">Child ID</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>List of spending by category</returns>
    [HttpGet("spending/{childId}")]
    public async Task<IActionResult> GetCategorySpending(
        Guid childId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var spending = await _categoryService.GetCategorySpendingAsync(childId, startDate, endDate);
        return Ok(spending);
    }

    /// <summary>
    /// Get budget status for all categories for a child
    /// </summary>
    /// <param name="childId">Child ID</param>
    /// <param name="period">Budget period (Weekly or Monthly)</param>
    /// <returns>List of budget statuses by category</returns>
    [HttpGet("budget-status/{childId}")]
    public async Task<IActionResult> GetBudgetStatus(
        Guid childId,
        [FromQuery] BudgetPeriod period = BudgetPeriod.Weekly)
    {
        var statuses = await _categoryService.GetBudgetStatusAsync(childId, period);
        return Ok(statuses);
    }

    /// <summary>
    /// Suggest a category based on transaction description
    /// </summary>
    /// <param name="description">Transaction description</param>
    /// <param name="type">Transaction type (Credit or Debit)</param>
    /// <returns>Suggested category</returns>
    [HttpGet("suggest")]
    public IActionResult SuggestCategory(
        [FromQuery] string description,
        [FromQuery] TransactionType type)
    {
        var category = _categoryService.SuggestCategory(description, type);
        return Ok(new CategorySuggestionResponse(category, _categoryService.GetCategoryDisplayName(category)));
    }
}

/// <summary>
/// Response DTO for category suggestion
/// </summary>
public record CategorySuggestionResponse(TransactionCategory Category, string DisplayName);
