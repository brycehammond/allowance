using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/savings-goals")]
[Authorize]
public class SavingsGoalsController : ControllerBase
{
    private readonly ISavingsGoalService _savingsGoalService;
    private readonly ICurrentUserService _currentUser;

    public SavingsGoalsController(
        ISavingsGoalService savingsGoalService,
        ICurrentUserService currentUser)
    {
        _savingsGoalService = savingsGoalService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all savings goals for a child
    /// </summary>
    [HttpGet("~/api/v1/children/{childId}/savings-goals")]
    [ProducesResponseType(typeof(List<SavingsGoalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SavingsGoalDto>>> GetChildGoals(
        Guid childId,
        [FromQuery] GoalStatus? status = null,
        [FromQuery] bool includeCompleted = false)
    {
        var goals = await _savingsGoalService.GetChildGoalsAsync(
            childId, status, includeCompleted, _currentUser.UserId);
        return Ok(goals);
    }

    /// <summary>
    /// Get a specific savings goal by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavingsGoalDto>> GetGoal(Guid id)
    {
        var goal = await _savingsGoalService.GetGoalByIdAsync(id, _currentUser.UserId);
        if (goal == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Goal not found" } });
        }
        return Ok(goal);
    }

    /// <summary>
    /// Create a new savings goal
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavingsGoalDto>> CreateGoal([FromBody] CreateSavingsGoalDto dto)
    {
        try
        {
            var goal = await _savingsGoalService.CreateGoalAsync(dto, _currentUser.UserId);
            return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, goal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = ex.Message } });
        }
    }

    /// <summary>
    /// Update a savings goal
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavingsGoalDto>> UpdateGoal(Guid id, [FromBody] UpdateSavingsGoalDto dto)
    {
        try
        {
            var goal = await _savingsGoalService.UpdateGoalAsync(id, dto, _currentUser.UserId);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    /// <summary>
    /// Cancel a savings goal (returns funds to balance)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGoal(Guid id)
    {
        try
        {
            await _savingsGoalService.CancelGoalAsync(id, _currentUser.UserId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    /// <summary>
    /// Pause a savings goal
    /// </summary>
    [HttpPost("{id}/pause")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavingsGoalDto>> PauseGoal(Guid id)
    {
        try
        {
            var goal = await _savingsGoalService.PauseGoalAsync(id, _currentUser.UserId);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    /// <summary>
    /// Resume a paused savings goal
    /// </summary>
    [HttpPost("{id}/resume")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavingsGoalDto>> ResumeGoal(Guid id)
    {
        try
        {
            var goal = await _savingsGoalService.ResumeGoalAsync(id, _currentUser.UserId);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    /// <summary>
    /// Contribute to a savings goal from child's balance
    /// </summary>
    [HttpPost("{id}/contribute")]
    [ProducesResponseType(typeof(GoalProgressEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoalProgressEventDto>> Contribute(Guid id, [FromBody] ContributeToGoalDto dto)
    {
        try
        {
            var result = await _savingsGoalService.ContributeAsync(id, dto, _currentUser.UserId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = ex.Message } });
        }
    }

    /// <summary>
    /// Withdraw from a savings goal back to balance
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(ContributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContributionDto>> Withdraw(Guid id, [FromBody] WithdrawFromGoalDto dto)
    {
        try
        {
            var result = await _savingsGoalService.WithdrawAsync(id, dto, _currentUser.UserId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get contribution history for a goal
    /// </summary>
    [HttpGet("{id}/contributions")]
    [ProducesResponseType(typeof(List<ContributionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ContributionDto>>> GetContributions(
        Guid id,
        [FromQuery] ContributionType? type = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var contributions = await _savingsGoalService.GetContributionsAsync(id, type, startDate, endDate);
        return Ok(contributions);
    }

    /// <summary>
    /// Mark a completed goal as purchased
    /// </summary>
    [HttpPost("{id}/purchase")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(SavingsGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavingsGoalDto>> MarkAsPurchased(Guid id, [FromBody] MarkGoalPurchasedDto dto)
    {
        try
        {
            var goal = await _savingsGoalService.MarkAsPurchasedAsync(id, dto, _currentUser.UserId);
            return Ok(goal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = ex.Message } });
        }
    }

    // ============= Matching Rules =============

    /// <summary>
    /// Create a parent matching rule for a goal
    /// </summary>
    [HttpPost("{id}/matching")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(MatchingRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MatchingRuleDto>> CreateMatching(Guid id, [FromBody] CreateMatchingRuleDto dto)
    {
        try
        {
            var rule = await _savingsGoalService.CreateMatchingRuleAsync(id, dto, _currentUser.UserId);
            return Ok(rule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get the matching rule for a goal
    /// </summary>
    [HttpGet("{id}/matching")]
    [ProducesResponseType(typeof(MatchingRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchingRuleDto>> GetMatching(Guid id)
    {
        var rule = await _savingsGoalService.GetMatchingRuleAsync(id);
        if (rule == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "No matching rule found" } });
        }
        return Ok(rule);
    }

    /// <summary>
    /// Update the matching rule for a goal
    /// </summary>
    [HttpPut("{id}/matching")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(MatchingRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchingRuleDto>> UpdateMatching(Guid id, [FromBody] UpdateMatchingRuleDto dto)
    {
        try
        {
            var rule = await _savingsGoalService.UpdateMatchingRuleAsync(id, dto, _currentUser.UserId);
            return Ok(rule);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    /// <summary>
    /// Remove the matching rule for a goal
    /// </summary>
    [HttpDelete("{id}/matching")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMatching(Guid id)
    {
        try
        {
            await _savingsGoalService.RemoveMatchingRuleAsync(id, _currentUser.UserId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    // ============= Challenges =============

    /// <summary>
    /// Create a challenge for a goal
    /// </summary>
    [HttpPost("{id}/challenge")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(GoalChallengeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoalChallengeDto>> CreateChallenge(Guid id, [FromBody] CreateGoalChallengeDto dto)
    {
        try
        {
            var challenge = await _savingsGoalService.CreateChallengeAsync(id, dto, _currentUser.UserId);
            return Ok(challenge);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get the active challenge for a goal
    /// </summary>
    [HttpGet("{id}/challenge")]
    [ProducesResponseType(typeof(GoalChallengeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoalChallengeDto>> GetChallenge(Guid id)
    {
        var challenge = await _savingsGoalService.GetActiveChallengeAsync(id);
        if (challenge == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "No active challenge found" } });
        }
        return Ok(challenge);
    }

    /// <summary>
    /// Cancel the active challenge for a goal
    /// </summary>
    [HttpDelete("{id}/challenge")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelChallenge(Guid id)
    {
        try
        {
            await _savingsGoalService.CancelChallengeAsync(id, _currentUser.UserId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = ex.Message } });
        }
    }

    /// <summary>
    /// Get all challenges for a child
    /// </summary>
    [HttpGet("~/api/v1/children/{childId}/challenges")]
    [ProducesResponseType(typeof(List<GoalChallengeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GoalChallengeDto>>> GetChildChallenges(Guid childId)
    {
        var challenges = await _savingsGoalService.GetChildChallengesAsync(childId, _currentUser.UserId);
        return Ok(challenges);
    }
}
