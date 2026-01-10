using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// Tracks progress toward earning a badge
/// </summary>
public class BadgeProgress : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public Guid BadgeId { get; set; }

    /// <summary>
    /// Current progress value (e.g., 7 out of 10 tasks)
    /// </summary>
    public int CurrentProgress { get; set; }

    /// <summary>
    /// Target progress value needed to earn the badge
    /// </summary>
    public int TargetProgress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
    public virtual Badge Badge { get; set; } = null!;
}
