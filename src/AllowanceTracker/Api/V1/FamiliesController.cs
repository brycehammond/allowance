using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/families")]
[Authorize]
public class FamiliesController : ControllerBase
{
    private readonly IFamilyService _familyService;
    private readonly IAccountService _accountService;

    public FamiliesController(IFamilyService familyService, IAccountService accountService)
    {
        _familyService = familyService;
        _accountService = accountService;
    }

    /// <summary>
    /// Get current user's family information
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<object>> GetCurrentFamily()
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var familyInfo = await _familyService.GetFamilyInfoAsync(currentUser.Id);
        if (familyInfo == null)
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

        return Ok(new
        {
            id = familyInfo.Id,
            name = familyInfo.Name,
            createdAt = familyInfo.CreatedAt,
            ownerId = familyInfo.OwnerId,
            ownerName = familyInfo.OwnerName,
            isCurrentUserOwner = familyInfo.OwnerId == currentUser.Id,
            memberCount = familyInfo.MemberCount,
            childrenCount = familyInfo.ChildrenCount
        });
    }

    /// <summary>
    /// Get all members of current user's family
    /// </summary>
    [HttpGet("current/members")]
    public async Task<ActionResult<object>> GetFamilyMembers()
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var familyMembers = await _familyService.GetFamilyMembersAsync(currentUser.Id);
        if (familyMembers == null)
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

        return Ok(new
        {
            familyId = familyMembers.FamilyId,
            familyName = familyMembers.FamilyName,
            members = familyMembers.Members.Select(m => new
            {
                userId = m.UserId,
                email = m.Email,
                firstName = m.FirstName,
                lastName = m.LastName,
                role = m.Role,
                isOwner = m.IsOwner
            })
        });
    }

    /// <summary>
    /// Get all children in current user's family
    /// </summary>
    [HttpGet("current/children")]
    public async Task<ActionResult<object>> GetFamilyChildren()
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var familyChildren = await _familyService.GetFamilyChildrenAsync(currentUser.Id);
        if (familyChildren == null)
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

        return Ok(new
        {
            familyId = familyChildren.FamilyId,
            familyName = familyChildren.FamilyName,
            children = familyChildren.Children.Select(c => new
            {
                childId = c.ChildId,
                userId = c.UserId,
                firstName = c.FirstName,
                lastName = c.LastName,
                email = c.Email,
                currentBalance = c.CurrentBalance,
                weeklyAllowance = c.WeeklyAllowance,
                lastAllowanceDate = c.LastAllowanceDate,
                nextAllowanceDate = c.NextAllowanceDate
            })
        });
    }

    /// <summary>
    /// Remove a co-parent from the family (owner only)
    /// </summary>
    /// <param name="userId">ID of the parent to remove</param>
    /// <returns>No content on success</returns>
    [HttpDelete("current/members/{userId:guid}")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveParent(Guid userId)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            await _familyService.RemoveParentAsync(userId, currentUser.Id);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "NOT_OWNER",
                    message = ex.Message
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "REMOVE_FAILED",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Transfer family ownership to another parent (owner only)
    /// </summary>
    /// <param name="dto">Contains the ID of the new owner</param>
    /// <returns>Updated family info</returns>
    [HttpPost("current/transfer-ownership")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FamilyInfoDto>> TransferOwnership([FromBody] TransferOwnershipDto dto)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _familyService.TransferOwnershipAsync(dto.NewOwnerId, currentUser.Id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "NOT_OWNER",
                    message = ex.Message
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "TRANSFER_FAILED",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Leave the family (non-owners only)
    /// </summary>
    /// <returns>No content on success</returns>
    [HttpPost("current/leave")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LeaveFamily()
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            await _familyService.LeaveFamilyAsync(currentUser.Id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "LEAVE_FAILED",
                    message = ex.Message
                }
            });
        }
    }
}
