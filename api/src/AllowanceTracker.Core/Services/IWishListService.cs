using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface IWishListService
{
    Task<WishListItem> CreateWishListItemAsync(CreateWishListItemDto dto);
    Task<List<WishListItemDto>> GetChildWishListAsync(Guid childId);
    Task<WishListItemDto?> GetWishListItemAsync(Guid id);
    Task<WishListItem?> UpdateWishListItemAsync(Guid id, UpdateWishListItemDto dto);
    Task<bool> DeleteWishListItemAsync(Guid id);
    Task<WishListItem?> MarkAsPurchasedAsync(Guid id);
    Task<WishListItem?> MarkAsUnpurchasedAsync(Guid id);
}
