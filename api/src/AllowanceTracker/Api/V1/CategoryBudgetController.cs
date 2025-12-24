using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/children/{childId}/budgets")]
[Authorize]
public class CategoryBudgetController : ControllerBase
{
    private readonly ICategoryBudgetService _budgetService;
    private readonly ICurrentUserService _currentUser;

    public CategoryBudgetController(
        ICategoryBudgetService budgetService,
        ICurrentUserService currentUser)
    {
        _budgetService = budgetService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all budgets for a child
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBudgets(Guid childId)
    {
        // Authorization check
        if (!await _budgetService.CanManageBudgetsAsync(childId, _currentUser.UserId))
        {
            return Forbid();
        }

        var budgets = await _budgetService.GetAllBudgetsAsync(childId);
        return Ok(budgets);
    }

    /// <summary>
    /// Get budget for a specific category
    /// </summary>
    [HttpGet("{category}")]
    public async Task<IActionResult> GetBudget(Guid childId, TransactionCategory category)
    {
        // Authorization check
        if (!await _budgetService.CanManageBudgetsAsync(childId, _currentUser.UserId))
        {
            return Forbid();
        }

        var budget = await _budgetService.GetBudgetAsync(childId, category);
        if (budget == null)
        {
            return NotFound();
        }
        return Ok(budget);
    }

    /// <summary>
    /// Create or update a budget
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> SetBudget([FromBody] SetBudgetDto dto)
    {
        var budget = await _budgetService.SetBudgetAsync(dto, _currentUser.UserId);
        return Ok(budget);
    }

    /// <summary>
    /// Delete a budget
    /// </summary>
    [HttpDelete("{category}")]
    public async Task<IActionResult> DeleteBudget(Guid childId, TransactionCategory category)
    {
        // Authorization check
        if (!await _budgetService.CanManageBudgetsAsync(childId, _currentUser.UserId))
        {
            return Forbid();
        }

        // Find the budget first
        var budget = await _budgetService.GetBudgetAsync(childId, category);
        if (budget == null)
        {
            return NotFound();
        }

        await _budgetService.DeleteBudgetAsync(budget.Id, _currentUser.UserId);
        return NoContent();
    }
}
