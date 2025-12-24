namespace AllowanceTracker.DTOs;

public record WishListItemDto(
    Guid Id,
    Guid ChildId,
    string Name,
    decimal Price,
    string? Url,
    string? Notes,
    bool IsPurchased,
    DateTime? PurchasedAt,
    DateTime CreatedAt,
    bool CanAfford
);
