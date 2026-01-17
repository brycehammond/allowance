using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// A gift submitted by an external family member through a gift link
/// </summary>
public class Gift : IHasCreatedAt
{
    public Guid Id { get; set; }

    /// <summary>
    /// The gift link this gift was submitted through
    /// </summary>
    public Guid GiftLinkId { get; set; }

    /// <summary>
    /// The child receiving the gift
    /// </summary>
    public Guid ChildId { get; set; }

    // Giver information (no account required)
    /// <summary>
    /// Name of the person giving the gift
    /// </summary>
    public string GiverName { get; set; } = string.Empty;

    /// <summary>
    /// Optional email of the giver (for confirmations and thank you notes)
    /// </summary>
    public string? GiverEmail { get; set; }

    /// <summary>
    /// Optional relationship to the child (e.g., "Grandma", "Uncle Bob")
    /// </summary>
    public string? GiverRelationship { get; set; }

    // Gift details
    /// <summary>
    /// The gift amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The occasion for the gift
    /// </summary>
    public GiftOccasion Occasion { get; set; }

    /// <summary>
    /// Optional custom occasion name (when Occasion = Other)
    /// </summary>
    public string? CustomOccasion { get; set; }

    /// <summary>
    /// Optional personal message from the giver
    /// </summary>
    public string? Message { get; set; }

    // Status tracking
    /// <summary>
    /// Current status of the gift
    /// </summary>
    public GiftStatus Status { get; set; } = GiftStatus.Pending;

    /// <summary>
    /// Reason for rejection (if Status = Rejected)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Parent who approved/rejected the gift
    /// </summary>
    public Guid? ProcessedById { get; set; }

    /// <summary>
    /// When the gift was approved/rejected
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    // Allocation options (set by parent during approval)
    /// <summary>
    /// Savings goal to allocate the gift to (null = spending balance)
    /// </summary>
    public Guid? AllocateToGoalId { get; set; }

    /// <summary>
    /// Percentage of gift to put in savings (0-100, null = all to spending)
    /// </summary>
    public int? SavingsPercentage { get; set; }

    /// <summary>
    /// The transaction created when gift was approved
    /// </summary>
    public Guid? TransactionId { get; set; }

    // Timestamps
    /// <summary>
    /// When the gift was submitted
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual GiftLink GiftLink { get; set; } = null!;
    public virtual Child Child { get; set; } = null!;
    public virtual ApplicationUser? ProcessedBy { get; set; }
    public virtual SavingsGoal? AllocateToGoal { get; set; }
    public virtual Transaction? Transaction { get; set; }
    public virtual ThankYouNote? ThankYouNote { get; set; }
}
