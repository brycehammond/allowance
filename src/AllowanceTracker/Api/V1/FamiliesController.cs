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
                role = m.Role
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
}
