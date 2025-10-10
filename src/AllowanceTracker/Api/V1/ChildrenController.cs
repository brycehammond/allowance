using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/children")]
[Authorize]
public class ChildrenController : ControllerBase
{
    private readonly IChildManagementService _childManagementService;
    private readonly IAccountService _accountService;

    public ChildrenController(IChildManagementService childManagementService, IAccountService accountService)
    {
        _childManagementService = childManagementService;
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

        var child = await _childManagementService.GetChildAsync(childId, currentUser.Id);

        if (child == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "Child not found or access denied"
                }
            });
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
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var child = await _childManagementService.UpdateChildAllowanceAsync(childId, dto.WeeklyAllowance, currentUser.Id);

        if (child == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "Child not found or access denied"
                }
            });
        }

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
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var deleted = await _childManagementService.DeleteChildAsync(childId, currentUser.Id);

        if (!deleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "Child not found or access denied"
                }
            });
        }

        return NoContent();
    }
}
