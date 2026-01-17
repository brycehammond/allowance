using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

/// <summary>
/// Request model for creating a gift link
/// </summary>
public class CreateGiftLinkRequest
{
    public Guid ChildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GiftLinkVisibility Visibility { get; set; } = GiftLinkVisibility.Minimal;
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public GiftOccasion? DefaultOccasion { get; set; }
}

[ApiController]
[Route("api/v1/gift-links")]
[Authorize]
public class GiftLinksController : ControllerBase
{
    private readonly IGiftLinkService _giftLinkService;
    private readonly ICurrentUserService _currentUserService;

    public GiftLinksController(IGiftLinkService giftLinkService, ICurrentUserService currentUserService)
    {
        _giftLinkService = giftLinkService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all gift links for the family
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<GiftLinkDto>>> GetFamilyLinks()
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var familyId = _currentUserService.FamilyId;
        if (familyId == null)
            return BadRequest("User is not part of a family");

        var links = await _giftLinkService.GetFamilyLinksAsync(familyId.Value);
        return Ok(links);
    }

    /// <summary>
    /// Create a new gift link
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GiftLinkDto>> CreateLink([FromBody] CreateGiftLinkRequest request)
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var dto = new CreateGiftLinkDto(
            request.ChildId,
            request.Name,
            request.Description,
            request.Visibility,
            request.ExpiresAt,
            request.MaxUses,
            request.MinAmount,
            request.MaxAmount,
            request.DefaultOccasion
        );

        try
        {
            var link = await _giftLinkService.CreateLinkAsync(dto, _currentUserService.UserId);
            return CreatedAtAction(nameof(GetLink), new { id = link.Id }, link);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get a specific gift link
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GiftLinkDto>> GetLink(Guid id)
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var link = await _giftLinkService.GetLinkByIdAsync(id);
        if (link == null)
            return NotFound();

        return Ok(link);
    }

    /// <summary>
    /// Update a gift link
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<GiftLinkDto>> UpdateLink(Guid id, [FromBody] UpdateGiftLinkDto dto)
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var link = await _giftLinkService.UpdateLinkAsync(id, dto);
        if (link == null)
            return NotFound();

        return Ok(link);
    }

    /// <summary>
    /// Deactivate a gift link
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult> DeactivateLink(Guid id)
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var result = await _giftLinkService.DeactivateLinkAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Regenerate the token for a gift link
    /// </summary>
    [HttpPost("{id}/regenerate-token")]
    public async Task<ActionResult<GiftLinkDto>> RegenerateToken(Guid id)
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var link = await _giftLinkService.RegenerateTokenAsync(id);
        if (link == null)
            return NotFound();

        return Ok(link);
    }

    /// <summary>
    /// Get statistics for a gift link
    /// </summary>
    [HttpGet("{id}/stats")]
    public async Task<ActionResult<GiftLinkStatsDto>> GetLinkStats(Guid id)
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var stats = await _giftLinkService.GetLinkStatsAsync(id);
        if (stats == null)
            return NotFound();

        return Ok(stats);
    }
}
