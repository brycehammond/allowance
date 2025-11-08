using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// Budget limits for specific categories per child
/// </summary>
public class CategoryBudget : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public TransactionCategory Category { get; set; }

    /// <summary>
    /// Budget limit per period (e.g., $20/week for Snacks)
    /// </summary>
    public decimal Limit { get; set; }

    /// <summary>
    /// Budget period (Weekly, Monthly)
    /// </summary>
    public BudgetPeriod Period { get; set; }

    /// <summary>
    /// Alert when spending reaches X% of limit (e.g., 80%)
    /// </summary>
    public int AlertThresholdPercent { get; set; } = 80;

    /// <summary>
    /// Enforce hard limit (prevent transactions over budget)
    /// </summary>
    public bool EnforceLimit { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedById { get; set; }

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}
