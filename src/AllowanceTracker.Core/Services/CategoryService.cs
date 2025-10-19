using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class CategoryService : ICategoryService
{
    private readonly AllowanceContext _context;

    public CategoryService(AllowanceContext context)
    {
        _context = context;
    }

    public async Task<List<CategorySpendingDto>> GetCategorySpendingAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Where(t => t.ChildId == childId && t.Type == TransactionType.Debit);

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= endDate.Value);
        }

        var spending = await query
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .ToListAsync();

        var totalSpending = spending.Sum(s => s.TotalAmount);

        return spending
            .OrderByDescending(s => s.TotalAmount)
            .ThenByDescending(s => s.Category)
            .Select(s => new CategorySpendingDto(
                s.Category,
                s.Category.ToString(),
                s.TotalAmount,
                s.TransactionCount,
                totalSpending > 0 ? (s.TotalAmount / totalSpending) * 100 : 0))
            .ToList();
    }

    public async Task<List<CategoryBudgetStatusDto>> GetBudgetStatusAsync(
        Guid childId,
        BudgetPeriod period)
    {
        var budgets = await _context.CategoryBudgets
            .Where(b => b.ChildId == childId && b.Period == period)
            .ToListAsync();

        var result = new List<CategoryBudgetStatusDto>();

        foreach (var budget in budgets)
        {
            var periodStart = GetPeriodStartDate(period);
            var spending = await _context.Transactions
                .Where(t => t.ChildId == childId &&
                           t.Category == budget.Category &&
                           t.Type == TransactionType.Debit &&
                           t.CreatedAt >= periodStart)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var remaining = budget.Limit - spending;
            var percentUsed = budget.Limit > 0 ? (int)((spending / budget.Limit) * 100) : 0;

            var status = GetBudgetStatus(percentUsed, budget.AlertThresholdPercent);

            result.Add(new CategoryBudgetStatusDto(
                budget.Category,
                budget.Category.ToString(),
                budget.Limit,
                spending,
                remaining,
                percentUsed,
                status,
                period));
        }

        return result;
    }

    public async Task<BudgetCheckResult> CheckBudgetAsync(
        Guid childId,
        TransactionCategory category,
        decimal amount)
    {
        var budget = await _context.CategoryBudgets
            .FirstOrDefaultAsync(b => b.ChildId == childId && b.Category == category);

        if (budget == null || !budget.EnforceLimit)
        {
            return new BudgetCheckResult(
                true,
                "No budget limit set for this category",
                0m,
                0m,
                0m);
        }

        var periodStart = GetPeriodStartDate(budget.Period);
        var currentSpending = await _context.Transactions
            .Where(t => t.ChildId == childId &&
                       t.Category == category &&
                       t.Type == TransactionType.Debit &&
                       t.CreatedAt >= periodStart)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var remainingAfter = budget.Limit - (currentSpending + amount);

        if (remainingAfter < 0)
        {
            return new BudgetCheckResult(
                false,
                $"This transaction exceeds budget limit. Budget: ${budget.Limit:F2}, Current: ${currentSpending:F2}, Transaction: ${amount:F2}",
                currentSpending,
                budget.Limit,
                remainingAfter);
        }

        return new BudgetCheckResult(
            true,
            "Transaction within budget",
            currentSpending,
            budget.Limit,
            remainingAfter);
    }

    public TransactionCategory SuggestCategory(string description, TransactionType type)
    {
        var lowerDesc = description.ToLowerInvariant();

        if (type == TransactionType.Credit)
        {
            if (lowerDesc.Contains("allowance")) return TransactionCategory.Allowance;
            if (lowerDesc.Contains("chore")) return TransactionCategory.Chores;
            if (lowerDesc.Contains("gift")) return TransactionCategory.Gift;
            if (lowerDesc.Contains("bonus") || lowerDesc.Contains("reward")) return TransactionCategory.BonusReward;
            return TransactionCategory.OtherIncome;
        }
        else // Debit
        {
            if (lowerDesc.Contains("toy")) return TransactionCategory.Toys;
            if (lowerDesc.Contains("game") || lowerDesc.Contains("video game")) return TransactionCategory.Games;
            if (lowerDesc.Contains("book")) return TransactionCategory.Books;
            if (lowerDesc.Contains("cloth") || lowerDesc.Contains("shirt") || lowerDesc.Contains("pants")) return TransactionCategory.Clothes;
            if (lowerDesc.Contains("snack")) return TransactionCategory.Snacks;
            if (lowerDesc.Contains("candy") || lowerDesc.Contains("sweet")) return TransactionCategory.Candy;
            if (lowerDesc.Contains("electronic") || lowerDesc.Contains("phone") || lowerDesc.Contains("tablet")) return TransactionCategory.Electronics;
            if (lowerDesc.Contains("entertainment") || lowerDesc.Contains("movie")) return TransactionCategory.Entertainment;
            if (lowerDesc.Contains("sport") || lowerDesc.Contains("ball")) return TransactionCategory.Sports;
            if (lowerDesc.Contains("craft") || lowerDesc.Contains("art")) return TransactionCategory.Crafts;
            if (lowerDesc.Contains("saving")) return TransactionCategory.Savings;
            if (lowerDesc.Contains("charity") || lowerDesc.Contains("donate")) return TransactionCategory.Charity;
            return TransactionCategory.OtherSpending;
        }
    }

    public List<TransactionCategory> GetCategoriesForType(TransactionType type)
    {
        if (type == TransactionType.Credit)
        {
            return new List<TransactionCategory>
            {
                TransactionCategory.Allowance,
                TransactionCategory.Chores,
                TransactionCategory.Gift,
                TransactionCategory.BonusReward,
                TransactionCategory.OtherIncome
            };
        }
        else // Debit
        {
            return new List<TransactionCategory>
            {
                TransactionCategory.Toys,
                TransactionCategory.Games,
                TransactionCategory.Books,
                TransactionCategory.Clothes,
                TransactionCategory.Snacks,
                TransactionCategory.Candy,
                TransactionCategory.Electronics,
                TransactionCategory.Entertainment,
                TransactionCategory.Sports,
                TransactionCategory.Crafts,
                TransactionCategory.Savings,
                TransactionCategory.Charity,
                TransactionCategory.OtherSpending
            };
        }
    }

    public List<TransactionCategory> GetAllCategories()
    {
        return Enum.GetValues<TransactionCategory>().ToList();
    }

    public string GetCategoryDisplayName(TransactionCategory category)
    {
        // Convert enum name to friendly display name (e.g., OtherIncome -> Other Income)
        return System.Text.RegularExpressions.Regex.Replace(
            category.ToString(),
            "([a-z])([A-Z])",
            "$1 $2");
    }

    private DateTime GetPeriodStartDate(BudgetPeriod period)
    {
        var now = DateTime.UtcNow;

        return period switch
        {
            BudgetPeriod.Weekly => now.AddDays(-7),
            BudgetPeriod.Monthly => now.AddMonths(-1),
            _ => now.AddDays(-7)
        };
    }

    private BudgetStatus GetBudgetStatus(int percentUsed, int alertThreshold)
    {
        if (percentUsed > 100)
            return BudgetStatus.OverBudget;
        if (percentUsed == 100)
            return BudgetStatus.AtLimit;
        if (percentUsed >= alertThreshold)
            return BudgetStatus.Warning;
        return BudgetStatus.Safe;
    }
}
