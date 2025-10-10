using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/children")]
[Authorize]
public class ChildrenController : ControllerBase
{
    private readonly AllowanceContext _context;
    private readonly IAccountService _accountService;

    public ChildrenController(AllowanceContext context, IAccountService accountService)
    {
        _context = context;
        _accountService = accountService;
    }

    /// <summary>
    /// Get child by ID
    /// </summary>
    [HttpGet("{childId}")]
    public async Task<ActionResult<object>> GetChild(Guid childId)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "Child not found"
                }
            });
        }

        // Check authorization: parent in same family or the child themselves
        if (currentUser.Role == UserRole.Parent)
        {
            if (child.FamilyId != currentUser.FamilyId)
            {
                return Forbid();
            }
        }
        else if (currentUser.Role == UserRole.Child)
        {
            if (child.UserId != currentUser.Id)
            {
                return Forbid();
            }
        }

        var nextAllowanceDate = child.LastAllowanceDate?.AddDays(7);

        return Ok(new
        {
            childId = child.Id,
            userId = child.UserId,
            firstName = child.User.FirstName,
            lastName = child.User.LastName,
            email = child.User.Email,
            currentBalance = child.CurrentBalance,
            weeklyAllowance = child.WeeklyAllowance,
            lastAllowanceDate = child.LastAllowanceDate,
            nextAllowanceDate = nextAllowanceDate,
            createdAt = child.CreatedAt
        });
    }

    /// <summary>
    /// Update child's weekly allowance (Parent only)
    /// </summary>
    [HttpPut("{childId}/allowance")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<object>> UpdateAllowance(Guid childId, [FromBody] UpdateAllowanceDto dto)
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

        var child = await _context.Children.FindAsync(childId);

        if (child == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "Child not found"
                }
            });
        }

        // Check same family
        if (child.FamilyId != currentUser.FamilyId)
        {
            return Forbid();
        }

        child.WeeklyAllowance = dto.WeeklyAllowance;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            childId = child.Id,
            weeklyAllowance = child.WeeklyAllowance,
            message = "Weekly allowance updated successfully"
        });
    }

    /// <summary>
    /// Delete child from family (Parent only)
    /// </summary>
    [HttpDelete("{childId}")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult> DeleteChild(Guid childId)
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

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "Child not found"
                }
            });
        }

        // Check same family
        if (child.FamilyId != currentUser.FamilyId)
        {
            return Forbid();
        }

        // Remove child profile and user
        _context.Children.Remove(child);
        _context.Users.Remove(child.User);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
