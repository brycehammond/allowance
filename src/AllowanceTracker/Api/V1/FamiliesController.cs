using AllowanceTracker.Data;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/families")]
[Authorize]
public class FamiliesController : ControllerBase
{
    private readonly AllowanceContext _context;
    private readonly IAccountService _accountService;

    public FamiliesController(AllowanceContext context, IAccountService accountService)
    {
        _context = context;
        _accountService = accountService;
    }

    /// <summary>
    /// Get current user's family information
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<object>> GetCurrentFamily()
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

        var family = await _context.Families
            .Include(f => f.Members)
            .Include(f => f.Children)
            .FirstAsync(f => f.Id == currentUser.FamilyId);

        return Ok(new
        {
            id = family.Id,
            name = family.Name,
            createdAt = family.CreatedAt,
            memberCount = family.Members.Count,
            childrenCount = family.Children.Count
        });
    }

    /// <summary>
    /// Get all members of current user's family
    /// </summary>
    [HttpGet("current/members")]
    public async Task<ActionResult<object>> GetFamilyMembers()
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

        var family = await _context.Families
            .Include(f => f.Members)
            .FirstAsync(f => f.Id == currentUser.FamilyId);

        var members = family.Members.Select(m => new
        {
            userId = m.Id,
            email = m.Email,
            firstName = m.FirstName,
            lastName = m.LastName,
            role = m.Role.ToString()
        });

        return Ok(new
        {
            familyId = family.Id,
            familyName = family.Name,
            members = members
        });
    }

    /// <summary>
    /// Get all children in current user's family
    /// </summary>
    [HttpGet("current/children")]
    public async Task<ActionResult<object>> GetFamilyChildren()
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

        var family = await _context.Families
            .Include(f => f.Children)
                .ThenInclude(c => c.User)
            .FirstAsync(f => f.Id == currentUser.FamilyId);

        var children = family.Children.Select(c => new
        {
            childId = c.Id,
            userId = c.UserId,
            firstName = c.User.FirstName,
            lastName = c.User.LastName,
            email = c.User.Email,
            currentBalance = c.CurrentBalance,
            weeklyAllowance = c.WeeklyAllowance,
            lastAllowanceDate = c.LastAllowanceDate,
            nextAllowanceDate = c.LastAllowanceDate?.AddDays(7)
        });

        return Ok(new
        {
            familyId = family.Id,
            familyName = family.Name,
            children = children
        });
    }
}
