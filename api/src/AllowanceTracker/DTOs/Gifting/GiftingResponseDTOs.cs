using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs.Gifting;

/// <summary>
/// Response DTO for a gift link
/// </summary>
public record GiftLinkDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    string Token,
    string Name,
    string? Description,
    GiftLinkVisibility Visibility,
    bool IsActive,
    DateTime? ExpiresAt,
    int? MaxUses,
    int UseCount,
    decimal? MinAmount,
    decimal? MaxAmount,
    GiftOccasion? DefaultOccasion,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ShareableUrl
);

/// <summary>
/// Response DTO for a gift
/// </summary>
public record GiftDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    string GiverName,
    string? GiverEmail,
    string? GiverRelationship,
    decimal Amount,
    GiftOccasion Occasion,
    string? CustomOccasion,
    string? Message,
    GiftStatus Status,
    string? RejectionReason,
    Guid? ProcessedById,
    DateTime? ProcessedAt,
    Guid? AllocateToGoalId,
    string? AllocateToGoalName,
    int? SavingsPercentage,
    DateTime CreatedAt,
    bool HasThankYouNote
);

/// <summary>
/// Data shown to gift givers on the public portal
/// </summary>
public record GiftPortalDataDto(
    string ChildFirstName,
    string? ChildAvatarUrl,
    decimal? MinAmount,
    decimal? MaxAmount,
    GiftOccasion? DefaultOccasion,
    GiftLinkVisibility Visibility,
    List<PortalSavingsGoalDto>? SavingsGoals,
    List<PortalWishListItemDto>? WishList
);

/// <summary>
/// Savings goal info visible on portal (limited info)
/// </summary>
public record PortalSavingsGoalDto(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    double ProgressPercentage,
    string? ImageUrl
);

/// <summary>
/// Wish list item info visible on portal (limited info)
/// </summary>
public record PortalWishListItemDto(
    Guid Id,
    string Name,
    decimal Price,
    string? Url,
    string? ImageUrl
);

/// <summary>
/// Response after submitting a gift
/// </summary>
public record GiftSubmissionResultDto(
    Guid GiftId,
    string ChildFirstName,
    decimal Amount,
    string Message
);

/// <summary>
/// Response DTO for a thank you note
/// </summary>
public record ThankYouNoteDto(
    Guid Id,
    Guid GiftId,
    Guid ChildId,
    string ChildName,
    string GiverName,
    string Message,
    string? ImageUrl,
    bool IsSent,
    DateTime? SentAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Summary of pending thank you notes for a child
/// </summary>
public record PendingThankYouDto(
    Guid GiftId,
    string GiverName,
    string? GiverRelationship,
    decimal Amount,
    GiftOccasion Occasion,
    string? CustomOccasion,
    DateTime ReceivedAt,
    int DaysSinceReceived,
    bool HasNote
);

/// <summary>
/// Statistics for gift links
/// </summary>
public record GiftLinkStatsDto(
    Guid GiftLinkId,
    int TotalGifts,
    int PendingGifts,
    int ApprovedGifts,
    int RejectedGifts,
    decimal TotalAmountReceived,
    DateTime? LastGiftAt
);
