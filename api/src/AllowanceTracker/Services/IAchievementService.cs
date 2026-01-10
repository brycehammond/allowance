using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

/// <summary>
/// Service for managing achievement badges and rewards
/// </summary>
public interface IAchievementService
{
    // Badge queries
    Task<List<BadgeDto>> GetAllBadgesAsync(BadgeCategory? category = null, bool includeSecret = false);
    Task<List<ChildBadgeDto>> GetChildBadgesAsync(Guid childId, BadgeCategory? category = null, bool newOnly = false);
    Task<List<BadgeProgressDto>> GetBadgeProgressAsync(Guid childId);
    Task<AchievementSummaryDto> GetAchievementSummaryAsync(Guid childId);

    // Badge mutations
    Task<ChildBadgeDto> ToggleBadgeDisplayAsync(Guid childId, Guid badgeId, bool isDisplayed);
    Task MarkBadgesSeenAsync(Guid childId, List<Guid> badgeIds);

    // Badge unlocking
    Task<ChildBadgeDto?> TryUnlockBadgeAsync(Guid childId, string badgeCode, string? context = null);
    Task CheckAndUnlockBadgesAsync(Guid childId, BadgeTrigger trigger, object? triggerData = null);

    // Points
    Task<ChildPointsDto> GetChildPointsAsync(Guid childId);

    // Rewards
    Task<List<RewardDto>> GetAvailableRewardsAsync(RewardType? type = null, Guid? childId = null);
    Task<List<RewardDto>> GetChildRewardsAsync(Guid childId);
    Task<RewardDto> UnlockRewardAsync(Guid childId, Guid rewardId);
    Task<RewardDto> EquipRewardAsync(Guid childId, Guid rewardId);
    Task UnequipRewardAsync(Guid childId, Guid rewardId);

    // Progress tracking
    Task UpdateProgressAsync(Guid childId, string badgeCode, int increment = 1);
    Task SetProgressAsync(Guid childId, string badgeCode, int value);
}
