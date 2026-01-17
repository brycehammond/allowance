using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface IGiftLinkService
{
    /// <summary>
    /// Creates a new gift link for a child
    /// </summary>
    Task<GiftLinkDto> CreateLinkAsync(CreateGiftLinkDto dto, Guid createdById);

    /// <summary>
    /// Gets all gift links for a family
    /// </summary>
    Task<List<GiftLinkDto>> GetFamilyLinksAsync(Guid familyId);

    /// <summary>
    /// Gets a gift link by ID
    /// </summary>
    Task<GiftLinkDto?> GetLinkByIdAsync(Guid linkId);

    /// <summary>
    /// Updates a gift link
    /// </summary>
    Task<GiftLinkDto?> UpdateLinkAsync(Guid linkId, UpdateGiftLinkDto dto);

    /// <summary>
    /// Deactivates a gift link
    /// </summary>
    Task<bool> DeactivateLinkAsync(Guid linkId);

    /// <summary>
    /// Regenerates the token for a gift link, invalidating the old one
    /// </summary>
    Task<GiftLinkDto?> RegenerateTokenAsync(Guid linkId);

    /// <summary>
    /// Validates a gift link token and returns the link if valid
    /// </summary>
    Task<GiftLink?> ValidateTokenAsync(string token);

    /// <summary>
    /// Gets statistics for a gift link
    /// </summary>
    Task<GiftLinkStatsDto?> GetLinkStatsAsync(Guid linkId);

    /// <summary>
    /// Increments the usage count for a gift link
    /// </summary>
    Task IncrementUsageCountAsync(Guid linkId);
}
