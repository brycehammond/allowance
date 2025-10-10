using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/wishlist")]
[Authorize]
public class WishListController : ControllerBase
{
    private readonly IWishListService _wishListService;

    public WishListController(IWishListService wishListService)
    {
        _wishListService = wishListService;
    }

    /// <summary>
    /// Get all wish list items for a child
    /// </summary>
    [HttpGet("children/{childId}")]
    public async Task<ActionResult<List<WishListItemDto>>> GetChildWishList(Guid childId)
    {
        var items = await _wishListService.GetChildWishListAsync(childId);
        return Ok(items);
    }

    /// <summary>
    /// Get a specific wish list item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WishListItemDto>> GetWishListItem(Guid id)
    {
        var item = await _wishListService.GetWishListItemAsync(id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    /// <summary>
    /// Create a new wish list item
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Models.WishListItem>> CreateWishListItem([FromBody] CreateWishListItemDto dto)
    {
        var item = await _wishListService.CreateWishListItemAsync(dto);
        return CreatedAtAction(nameof(GetWishListItem), new { id = item.Id }, item);
    }

    /// <summary>
    /// Update a wish list item
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Models.WishListItem>> UpdateWishListItem(Guid id, [FromBody] UpdateWishListItemDto dto)
    {
        var item = await _wishListService.UpdateWishListItemAsync(id, dto);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    /// <summary>
    /// Delete a wish list item
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteWishListItem(Guid id)
    {
        var deleted = await _wishListService.DeleteWishListItemAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Mark a wish list item as purchased
    /// </summary>
    [HttpPost("{id}/purchase")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<Models.WishListItem>> MarkAsPurchased(Guid id)
    {
        var item = await _wishListService.MarkAsPurchasedAsync(id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    /// <summary>
    /// Mark a wish list item as unpurchased
    /// </summary>
    [HttpPost("{id}/unpurchase")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<Models.WishListItem>> MarkAsUnpurchased(Guid id)
    {
        var item = await _wishListService.MarkAsUnpurchasedAsync(id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }
}
