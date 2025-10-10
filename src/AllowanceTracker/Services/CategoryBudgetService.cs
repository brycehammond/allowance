using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class CategoryBudgetService : ICategoryBudgetService
{
    private readonly AllowanceContext _context;

    public CategoryBudgetService(AllowanceContext context)
    {
        _context = context;
    }

    public async Task<CategoryBudget> SetBudgetAsync(SetBudgetDto dto, Guid currentUserId)
    {
        // Check if child exists first
        var child = await _context.Children.FindAsync(dto.ChildId);
        if (child == null)
        {
            throw new InvalidOperationException("Child not found");
        }

        // Check if user can manage budgets for this child
        if (!await CanManageBudgetsAsync(dto.ChildId, currentUserId))
        {
            throw new UnauthorizedAccessException("User is not authorized to manage budgets for this child");
        }

        // Check if budget already exists for this category
        var existingBudget = await _context.CategoryBudgets
            .FirstOrDefaultAsync(b => b.ChildId == dto.ChildId && b.Category == dto.Category);

        if (existingBudget != null)
        {
            // Update existing budget
            existingBudget.Limit = dto.Limit;
            existingBudget.Period = dto.Period;
            existingBudget.AlertThresholdPercent = dto.AlertThresholdPercent;
            existingBudget.EnforceLimit = dto.EnforceLimit;
            existingBudget.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingBudget;
        }
        else
        {
            // Create new budget
            var budget = new CategoryBudget
            {
                Id = Guid.NewGuid(),
                ChildId = dto.ChildId,
                Category = dto.Category,
                Limit = dto.Limit,
                Period = dto.Period,
                AlertThresholdPercent = dto.AlertThresholdPercent,
                EnforceLimit = dto.EnforceLimit,
                CreatedById = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CategoryBudgets.Add(budget);
            await _context.SaveChangesAsync();
            return budget;
        }
    }

    public async Task<CategoryBudget?> GetBudgetAsync(Guid childId, TransactionCategory category)
    {
        return await _context.CategoryBudgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ChildId == childId && b.Category == category);
    }

    public async Task<List<CategoryBudget>> GetAllBudgetsAsync(Guid childId)
    {
        return await _context.CategoryBudgets
            .AsNoTracking()
            .Where(b => b.ChildId == childId)
            .OrderBy(b => b.Category)
            .ToListAsync();
    }

    public async Task DeleteBudgetAsync(Guid budgetId, Guid currentUserId)
    {
        var budget = await _context.CategoryBudgets
            .Include(b => b.Child)
            .FirstOrDefaultAsync(b => b.Id == budgetId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        // Check if user can manage budgets for this child
        if (!await CanManageBudgetsAsync(budget.ChildId, currentUserId))
        {
            throw new UnauthorizedAccessException("User is not authorized to manage budgets for this child");
        }

        _context.CategoryBudgets.Remove(budget);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CanManageBudgetsAsync(Guid childId, Guid userId)
    {
        // Get the child with their family
        var child = await _context.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            return false;
        }

        // Get the user
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return false;
        }

        // Only parents in the same family can manage budgets
        return user.Role == UserRole.Parent && user.FamilyId == child.FamilyId;
    }
}
