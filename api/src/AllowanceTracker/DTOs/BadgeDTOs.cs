using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Badge details for display
/// </summary>
public record BadgeDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string IconUrl,
    BadgeCategory Category,
    string CategoryName,
    BadgeRarity Rarity,
    string RarityName,
    int PointsValue,
    bool IsSecret,
    bool IsEarned,
    DateTime? EarnedAt,
    bool IsDisplayed,
    int? CurrentProgress,
    int? TargetProgress,
    double? ProgressPercentage
);

/// <summary>
/// Badge that a child has earned
/// </summary>
public record ChildBadgeDto(
    Guid Id,
    Guid BadgeId,
    string BadgeName,
    string BadgeDescription,
    string IconUrl,
    BadgeCategory Category,
    string CategoryName,
    BadgeRarity Rarity,
    string RarityName,
    int PointsValue,
    DateTime EarnedAt,
    bool IsDisplayed,
    bool IsNew,
    string? EarnedContext
);

/// <summary>
/// Progress toward earning a badge
/// </summary>
public record BadgeProgressDto(
    Guid BadgeId,
    string BadgeName,
    string Description,
    string IconUrl,
    BadgeCategory Category,
    string CategoryName,
    BadgeRarity Rarity,
    string RarityName,
    int PointsValue,
    int CurrentProgress,
    int TargetProgress,
    double ProgressPercentage,
    string ProgressText
);

/// <summary>
/// Child's points summary
/// </summary>
public record ChildPointsDto(
    int TotalPoints,
    int AvailablePoints,
    int SpentPoints,
    int BadgesEarned,
    int RewardsUnlocked
);

/// <summary>
/// Complete achievement summary for a child
/// </summary>
public record AchievementSummaryDto(
    int TotalBadges,
    int EarnedBadges,
    int TotalPoints,
    int AvailablePoints,
    List<ChildBadgeDto> RecentBadges,
    List<BadgeProgressDto> InProgressBadges,
    Dictionary<string, int> BadgesByCategory
);

/// <summary>
/// Event emitted when a badge is unlocked
/// </summary>
public record BadgeUnlockedEventDto(
    Guid ChildId,
    BadgeDto Badge,
    int NewTotalPoints,
    int NewAvailablePoints
);

/// <summary>
/// Request to toggle badge display on profile
/// </summary>
public record UpdateBadgeDisplayDto(
    bool IsDisplayed
);

/// <summary>
/// Request to mark badges as seen
/// </summary>
public record MarkBadgesSeenDto(
    List<Guid> BadgeIds
);
