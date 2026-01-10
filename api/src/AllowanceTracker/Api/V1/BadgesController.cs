using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

/// <summary>
/// Controller for managing achievement badges
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class BadgesController : ControllerBase
{
    private readonly IAchievementService _achievementService;

    public BadgesController(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    /// <summary>
    /// Get all available badges
    /// </summary>
    /// <param name="category">Optional: filter by badge category</param>
    /// <param name="includeSecret">Optional: include secret badges (default: false)</param>
    [HttpGet("badges")]
    public async Task<ActionResult<List<BadgeDto>>> GetAllBadges(
        [FromQuery] BadgeCategory? category = null,
        [FromQuery] bool includeSecret = false)
    {
        var badges = await _achievementService.GetAllBadgesAsync(category, includeSecret);
        return Ok(badges);
    }

    /// <summary>
    /// Get badges earned by a child
    /// </summary>
    /// <param name="childId">Child ID</param>
    /// <param name="category">Optional: filter by badge category</param>
    /// <param name="newOnly">Optional: only return unseen badges</param>
    [HttpGet("children/{childId}/badges")]
    public async Task<ActionResult<List<ChildBadgeDto>>> GetChildBadges(
        Guid childId,
        [FromQuery] BadgeCategory? category = null,
        [FromQuery] bool newOnly = false)
    {
        var badges = await _achievementService.GetChildBadgesAsync(childId, category, newOnly);
        return Ok(badges);
    }

    /// <summary>
    /// Get badge progress for a child
    /// </summary>
    [HttpGet("children/{childId}/badges/progress")]
    public async Task<ActionResult<List<BadgeProgressDto>>> GetBadgeProgress(Guid childId)
    {
        var progress = await _achievementService.GetBadgeProgressAsync(childId);
        return Ok(progress);
    }

    /// <summary>
    /// Get achievement summary for a child
    /// </summary>
    [HttpGet("children/{childId}/badges/summary")]
    public async Task<ActionResult<AchievementSummaryDto>> GetAchievementSummary(Guid childId)
    {
        var summary = await _achievementService.GetAchievementSummaryAsync(childId);
        return Ok(summary);
    }

    /// <summary>
    /// Toggle badge display on profile
    /// </summary>
    [HttpPatch("children/{childId}/badges/{badgeId}/display")]
    public async Task<ActionResult<ChildBadgeDto>> ToggleBadgeDisplay(
        Guid childId,
        Guid badgeId,
        [FromBody] UpdateBadgeDisplayDto dto)
    {
        try
        {
            var badge = await _achievementService.ToggleBadgeDisplayAsync(childId, badgeId, dto.IsDisplayed);
            return Ok(badge);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Mark badges as seen
    /// </summary>
    [HttpPost("children/{childId}/badges/seen")]
    public async Task<ActionResult> MarkBadgesSeen(
        Guid childId,
        [FromBody] MarkBadgesSeenDto dto)
    {
        await _achievementService.MarkBadgesSeenAsync(childId, dto.BadgeIds);
        return NoContent();
    }

    /// <summary>
    /// Get child's points summary
    /// </summary>
    [HttpGet("children/{childId}/points")]
    public async Task<ActionResult<ChildPointsDto>> GetChildPoints(Guid childId)
    {
        try
        {
            var points = await _achievementService.GetChildPointsAsync(childId);
            return Ok(points);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}
