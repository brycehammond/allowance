using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

/// <summary>
/// Controller for managing rewards that can be purchased with badge points
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class RewardsController : ControllerBase
{
    private readonly IAchievementService _achievementService;

    public RewardsController(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    /// <summary>
    /// Get all available rewards
    /// </summary>
    /// <param name="type">Optional: filter by reward type</param>
    /// <param name="childId">Optional: include affordability info for a specific child</param>
    [HttpGet("rewards")]
    public async Task<ActionResult<List<RewardDto>>> GetAvailableRewards(
        [FromQuery] RewardType? type = null,
        [FromQuery] Guid? childId = null)
    {
        var rewards = await _achievementService.GetAvailableRewardsAsync(type, childId);
        return Ok(rewards);
    }

    /// <summary>
    /// Get rewards unlocked by a child
    /// </summary>
    [HttpGet("children/{childId}/rewards")]
    public async Task<ActionResult<List<RewardDto>>> GetChildRewards(Guid childId)
    {
        var rewards = await _achievementService.GetChildRewardsAsync(childId);
        return Ok(rewards);
    }

    /// <summary>
    /// Unlock a reward with points
    /// </summary>
    [HttpPost("children/{childId}/rewards/{rewardId}/unlock")]
    public async Task<ActionResult<RewardDto>> UnlockReward(Guid childId, Guid rewardId)
    {
        try
        {
            var reward = await _achievementService.UnlockRewardAsync(childId, rewardId);
            return Ok(reward);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("insufficient points"))
        {
            return BadRequest(new { error = "Insufficient points to unlock this reward" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already unlocked"))
        {
            return Conflict(new { error = "Reward has already been unlocked" });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Equip an unlocked reward
    /// </summary>
    [HttpPost("children/{childId}/rewards/{rewardId}/equip")]
    public async Task<ActionResult<RewardDto>> EquipReward(Guid childId, Guid rewardId)
    {
        try
        {
            var reward = await _achievementService.EquipRewardAsync(childId, rewardId);
            return Ok(reward);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Unequip a reward
    /// </summary>
    [HttpPost("children/{childId}/rewards/{rewardId}/unequip")]
    public async Task<ActionResult> UnequipReward(Guid childId, Guid rewardId)
    {
        try
        {
            await _achievementService.UnequipRewardAsync(childId, rewardId);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}
