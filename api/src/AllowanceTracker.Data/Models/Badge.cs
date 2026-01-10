using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// Represents an achievement badge that children can earn
/// </summary>
public class Badge : IHasCreatedAt
{
    public Guid Id { get; set; }

    /// <summary>
    /// Unique identifier code for the badge (e.g., "FIRST_SAVER", "PENNY_PINCHER")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the badge
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of how to earn the badge
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// URL to the badge icon image
    /// </summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>
    /// Category this badge belongs to
    /// </summary>
    public BadgeCategory Category { get; set; }

    /// <summary>
    /// Rarity level of the badge
    /// </summary>
    public BadgeRarity Rarity { get; set; }

    /// <summary>
    /// Points awarded when badge is earned
    /// </summary>
    public int PointsValue { get; set; }

    /// <summary>
    /// Type of criteria used to determine when badge is unlocked
    /// </summary>
    public BadgeCriteriaType CriteriaType { get; set; }

    /// <summary>
    /// JSON configuration for the unlock criteria
    /// </summary>
    public string CriteriaConfig { get; set; } = "{}";

    /// <summary>
    /// If true, badge is hidden until earned
    /// </summary>
    public bool IsSecret { get; set; } = false;

    /// <summary>
    /// If false, badge is retired and cannot be earned
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Order for display sorting
    /// </summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ChildBadge> ChildBadges { get; set; } = new List<ChildBadge>();
    public virtual ICollection<BadgeProgress> BadgeProgress { get; set; } = new List<BadgeProgress>();
}
