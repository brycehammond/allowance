using AllowanceTracker.Data;

namespace AllowanceTracker.Models;

/// <summary>
/// Represents a reward that a child has unlocked
/// </summary>
public class ChildReward : IHasCreatedAt
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public Guid RewardId { get; set; }

    /// <summary>
    /// When the reward was unlocked
    /// </summary>
    public DateTime UnlockedAt { get; set; }

    /// <summary>
    /// Whether this reward is currently equipped/active
    /// </summary>
    public bool IsEquipped { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
    public virtual Reward Reward { get; set; } = null!;
}
