using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

public class SavingsContribution : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid GoalId { get; set; }
    public virtual SavingsGoal Goal { get; set; } = null!;

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public decimal Amount { get; set; }
    public ContributionType Type { get; set; }
    public decimal GoalBalanceAfter { get; set; }

    // Source tracking
    public Guid? SourceTransactionId { get; set; }  // If from main balance via a transaction
    public Guid? ParentMatchId { get; set; }        // If this is a match contribution, links to the original contribution

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public virtual ApplicationUser? CreatedBy { get; set; }
}
