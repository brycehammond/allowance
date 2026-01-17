using System.ComponentModel.DataAnnotations;
using AllowanceTracker.Models;

namespace AllowanceTracker.DTOs.Gifting;

public record SubmitGiftDto(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string GiverName,

    [EmailAddress]
    [StringLength(256)]
    string? GiverEmail,

    [StringLength(100)]
    string? GiverRelationship,

    [Required]
    [Range(0.01, 100000)]
    decimal Amount,

    GiftOccasion Occasion,

    [StringLength(100)]
    string? CustomOccasion,

    [StringLength(2000)]
    string? Message
);

public record ApproveGiftDto(
    Guid? AllocateToGoalId = null,

    [Range(0, 100)]
    int? SavingsPercentage = null
);

public record RejectGiftDto(
    [StringLength(500)]
    string? Reason = null
);
