using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// Tracks the history of allowance adjustments (pauses, resumes, amount changes)
/// </summary>
public class AllowanceAdjustment : IHasCreatedAt
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public AllowanceAdjustmentType AdjustmentType { get; set; }

    /// <summary>
    /// Previous allowance amount (for amount adjustments)
    /// </summary>
    public decimal? OldAmount { get; set; }

    /// <summary>
    /// New allowance amount (for amount adjustments)
    /// </summary>
    public decimal? NewAmount { get; set; }

    /// <summary>
    /// Optional reason for the adjustment
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// ID of the user (parent) who made the adjustment
    /// </summary>
    public Guid AdjustedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
    public virtual ApplicationUser AdjustedBy { get; set; } = null!;
}

/// <summary>
/// Type of allowance adjustment
/// </summary>
public enum AllowanceAdjustmentType
{
    /// <summary>
    /// Allowance was paused
    /// </summary>
    Paused,

    /// <summary>
    /// Allowance was resumed after being paused
    /// </summary>
    Resumed,

    /// <summary>
    /// Allowance amount was changed
    /// </summary>
    AmountChanged
}
