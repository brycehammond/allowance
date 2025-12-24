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
    [HttpGet("{category}/display-name")]
    public IActionResult GetCategoryDisplayName(TransactionCategory category)
    {
        var displayName = _categoryService.GetCategoryDisplayName(category);
        return Ok(displayName);
    }
}
