using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface IGiftService
{
    /// <summary>
    /// Gets portal data for a gift link (public, no auth required)
    /// </summary>
    Task<GiftPortalDataDto> GetPortalDataAsync(string token);

    /// <summary>
    /// Submits a gift through the portal (public, no auth required)
    /// </summary>
    Task<GiftSubmissionResultDto> SubmitGiftAsync(string token, SubmitGiftDto dto);

    /// <summary>
    /// Gets all pending gifts for a family
    /// </summary>
    Task<List<GiftDto>> GetPendingGiftsAsync(Guid familyId);

    /// <summary>
    /// Gets all gifts for a child
    /// </summary>
    Task<List<GiftDto>> GetChildGiftsAsync(Guid childId);

    /// <summary>
    /// Gets a gift by ID
    /// </summary>
    Task<GiftDto?> GetGiftByIdAsync(Guid giftId);

    /// <summary>
    /// Approves a gift, adding the amount to child's balance
    /// </summary>
    Task<GiftDto> ApproveGiftAsync(Guid giftId, ApproveGiftDto dto, Guid approvedById);

    /// <summary>
    /// Rejects a gift with optional reason
    /// </summary>
    Task<GiftDto> RejectGiftAsync(Guid giftId, RejectGiftDto dto, Guid rejectedById);

    /// <summary>
    /// Expires pending gifts older than specified days
    /// </summary>
    Task<int> ExpireOldPendingGiftsAsync(int daysOld = 30);
}
