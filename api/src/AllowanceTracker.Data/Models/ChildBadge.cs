using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// Represents a badge that a child has earned
/// </summary>
public class ChildBadge : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public Guid BadgeId { get; set; }

    /// <summary>
    /// When the badge was earned
    /// </summary>
    public DateTime EarnedAt { get; set; }

    /// <summary>
    /// Whether to display this badge on the child's profile
    /// </summary>
    public bool IsDisplayed { get; set; } = true;

    /// <summary>
    /// Whether the badge is new (unseen by the user)
    /// </summary>
    public bool IsNew { get; set; } = true;

    /// <summary>
    /// JSON containing context about how the badge was earned
    /// </summary>
    public string? EarnedContext { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
    public virtual Badge Badge { get; set; } = null!;
}
