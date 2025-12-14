using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/invites")]
public class ParentInviteController : ControllerBase
{
    private readonly IParentInviteService _inviteService;
    private readonly IAccountService _accountService;

    public ParentInviteController(
        IParentInviteService inviteService,
        IAccountService accountService)
    {
        _inviteService = inviteService;
        _accountService = accountService;
    }

    /// <summary>
    /// Send an invite to a co-parent
    /// </summary>
    /// <param name="dto">Invite details including email, first name, and last name</param>
    /// <returns>Invite details including whether this is for a new or existing user</returns>
    [HttpPost("parent")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ParentInviteResponseDto>> SendInvite([FromBody] SendParentInviteDto dto)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser?.FamilyId == null)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "NO_FAMILY",
                    message = "Current user has no associated family"
                }
            });
        }

        try
        {
            var result = await _inviteService.SendInviteAsync(dto, currentUser.Id, currentUser.FamilyId.Value);
            return CreatedAtAction(nameof(GetPendingInvites), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVITE_FAILED",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Validate an invite token
    /// </summary>
    /// <param name="token">Invite token from email</param>
    /// <param name="email">Email address</param>
    /// <returns>Validation result with invite details if valid</returns>
    [HttpGet("validate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ValidateInviteResponseDto>> ValidateToken([FromQuery] string token, [FromQuery] string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            return Ok(new ValidateInviteResponseDto(false, false, null, null, null, null, "Token and email are required."));
        }

        var result = await _inviteService.ValidateTokenAsync(token, email);
        return Ok(result);
    }

    /// <summary>
    /// Accept an invite and set password (for new users)
    /// </summary>
    /// <param name="dto">Accept invite details with password</param>
    /// <returns>Auth response with JWT token for auto-login</returns>
    [HttpPost("accept")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> AcceptInvite([FromBody] AcceptInviteDto dto)
    {
        try
        {
            var result = await _inviteService.AcceptNewUserInviteAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "ACCEPT_FAILED",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Accept a family join request (for existing users)
    /// </summary>
    /// <param name="dto">Join request with token</param>
    /// <returns>Join response with family details</returns>
    [HttpPost("accept-join")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AcceptJoinResponseDto>> AcceptJoinRequest([FromBody] AcceptJoinRequestDto dto)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _inviteService.AcceptJoinRequestAsync(dto.Token, currentUser.Id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "JOIN_FAILED",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Cancel a pending invite
    /// </summary>
    /// <param name="inviteId">ID of the invite to cancel</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{inviteId:guid}")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvite(Guid inviteId)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var result = await _inviteService.CancelInviteAsync(inviteId, currentUser.Id);
        if (!result)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "INVITE_NOT_FOUND",
                    message = "Invite not found or cannot be cancelled"
                }
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Get all pending invites for the current user's family
    /// </summary>
    /// <returns>List of pending invites</returns>
    [HttpGet]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ParentInviteDto>>> GetPendingInvites()
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser?.FamilyId == null)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "NO_FAMILY",
                    message = "Current user has no associated family"
                }
            });
        }

        var invites = await _inviteService.GetPendingInvitesAsync(currentUser.FamilyId.Value);
        return Ok(invites);
    }
}
