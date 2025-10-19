using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class WishListService : IWishListService
{
    private readonly AllowanceContext _context;

    public WishListService(AllowanceContext context)
    {
        _context = context;
    }

    public async Task<WishListItem> CreateWishListItemAsync(CreateWishListItemDto dto)
    {
        var item = new WishListItem
        {
            ChildId = dto.ChildId,
            Name = dto.Name,
            Price = dto.Price,
            Url = dto.Url,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.WishListItems.Add(item);
        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<List<WishListItemDto>> GetChildWishListAsync(Guid childId)
    {
        var child = await _context.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
            return new List<WishListItemDto>();

        var items = await _context.WishListItems
            .Where(w => w.ChildId == childId)
            .OrderBy(w => w.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return items.Select(i => new WishListItemDto(
            i.Id,
            i.ChildId,
            i.Name,
            i.Price,
            i.Url,
            i.Notes,
            i.IsPurchased,
            i.PurchasedAt,
            i.CreatedAt,
            i.CanAfford(child.CurrentBalance)
        )).ToList();
    }

    public async Task<WishListItemDto?> GetWishListItemAsync(Guid id)
    {
        var item = await _context.WishListItems
            .AsNoTracking()
            .Include(w => w.Child)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (item == null)
            return null;

        return new WishListItemDto(
            item.Id,
            item.ChildId,
            item.Name,
            item.Price,
            item.Url,
            item.Notes,
            item.IsPurchased,
            item.PurchasedAt,
            item.CreatedAt,
            item.CanAfford(item.Child.CurrentBalance)
        );
    }

    public async Task<WishListItem?> UpdateWishListItemAsync(Guid id, UpdateWishListItemDto dto)
    {
        var item = await _context.WishListItems.FindAsync(id);

        if (item == null)
            return null;

        item.Name = dto.Name;
        item.Price = dto.Price;
        item.Url = dto.Url;
        item.Notes = dto.Notes;

        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<bool> DeleteWishListItemAsync(Guid id)
    {
        var item = await _context.WishListItems.FindAsync(id);

        if (item == null)
            return false;

        _context.WishListItems.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<WishListItem?> MarkAsPurchasedAsync(Guid id)
    {
        var item = await _context.WishListItems.FindAsync(id);

        if (item == null)
            return null;

        item.IsPurchased = true;
        item.PurchasedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<WishListItem?> MarkAsUnpurchasedAsync(Guid id)
    {
        var item = await _context.WishListItems.FindAsync(id);

        if (item == null)
            return null;

        item.IsPurchased = false;
        item.PurchasedAt = null;

        await _context.SaveChangesAsync();

        return item;
    }
}
