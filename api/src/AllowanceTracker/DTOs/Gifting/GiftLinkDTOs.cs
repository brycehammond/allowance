using System.ComponentModel.DataAnnotations;
using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs.Gifting;

public record CreateGiftLinkDto(
    Guid ChildId,

    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description = null,

    GiftLinkVisibility Visibility = GiftLinkVisibility.Minimal,

    DateTime? ExpiresAt = null,

    int? MaxUses = null,

    [Range(0.01, 100000)]
    decimal? MinAmount = null,

    [Range(0.01, 100000)]
    decimal? MaxAmount = null,

    GiftOccasion? DefaultOccasion = null
);

public record UpdateGiftLinkDto(
    [StringLength(200, MinimumLength = 1)]
    string? Name = null,

    [StringLength(1000)]
    string? Description = null,

    GiftLinkVisibility? Visibility = null,

    bool? IsActive = null,

    DateTime? ExpiresAt = null,

    int? MaxUses = null,

    [Range(0.01, 100000)]
    decimal? MinAmount = null,

    [Range(0.01, 100000)]
    decimal? MaxAmount = null,

    GiftOccasion? DefaultOccasion = null
);
