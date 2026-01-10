using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Reward details for display
/// </summary>
public record RewardDto(
    Guid Id,
    string Name,
    string Description,
    RewardType Type,
    string TypeName,
    string Value,
    string? PreviewUrl,
    int PointsCost,
    bool IsUnlocked,
    bool IsEquipped,
    bool CanAfford
);

/// <summary>
/// Request to unlock a reward
/// </summary>
public record UnlockRewardDto(
    Guid RewardId
);

/// <summary>
/// Request to equip a reward
/// </summary>
public record EquipRewardDto(
    Guid RewardId
);
