using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class Transaction : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}
