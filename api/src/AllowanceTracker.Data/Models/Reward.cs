using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// Represents a reward that can be purchased with badge points
/// </summary>
public class Reward : IHasCreatedAt
{
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the reward
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the reward
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of reward
    /// </summary>
    public RewardType Type { get; set; }

    /// <summary>
    /// The reward value (URL for avatar/theme, or identifier for special rewards)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Preview image URL
    /// </summary>
    public string? PreviewUrl { get; set; }

    /// <summary>
    /// Points required to unlock this reward
    /// </summary>
    public int PointsCost { get; set; }

    /// <summary>
    /// Whether this reward is currently available for purchase
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Order for display sorting
    /// </summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ChildReward> ChildRewards { get; set; } = new List<ChildReward>();
}
