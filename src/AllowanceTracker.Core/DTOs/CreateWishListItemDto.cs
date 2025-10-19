using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

public record CreateWishListItemDto(
    Guid ChildId,

    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [Required]
    [Range(0.01, 100000)]
    decimal Price,

    [Url]
    string? Url,

    [StringLength(1000)]
    string? Notes
);
