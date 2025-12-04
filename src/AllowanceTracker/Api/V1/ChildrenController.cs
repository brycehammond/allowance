using AllowanceTracker.DTOs;
using AllowanceTracker.DTOs.Allowances;
using AllowanceTracker.Models;
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
    private readonly IFamilyService _familyService;
    private readonly ITransactionService _transactionService;
    private readonly IAllowanceService _allowanceService;

    public ChildrenController(
        IChildManagementService childManagementService,
        IAccountService accountService,
        IFamilyService familyService,
        ITransactionService transactionService,
        IAllowanceService allowanceService)
    {
        _childManagementService = childManagementService;
        _accountService = accountService;
        _familyService = familyService;
        _transactionService = transactionService;
        _allowanceService = allowanceService;
    }

    /// <summary>
    /// Get all children in current user's family (iOS parity)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ChildDto>>> GetChildren()
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

        var children = familyChildren.Children.Select(c => new ChildDto(
            c.ChildId,
            c.FirstName,
            c.LastName,
            c.WeeklyAllowance,
            c.CurrentBalance,
            c.LastAllowanceDate,
            c.AllowanceDay)).ToList();

        return Ok(children);
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

        // Determine if savings balance should be shown to requesting user
        var showSavingsBalance = currentUser.Role == UserRole.Parent || child.SavingsBalanceVisibleToChild;

        return Ok(new
        {
            id = child.Id,
            userId = child.UserId,
            firstName = child.User.FirstName,
            lastName = child.User.LastName,
            fullName = $"{child.User.FirstName} {child.User.LastName}",
            email = child.User.Email,
            currentBalance = child.CurrentBalance,
            weeklyAllowance = child.WeeklyAllowance,
            allowanceDay = child.AllowanceDay,
            lastAllowanceDate = child.LastAllowanceDate,
            nextAllowanceDate = nextAllowanceDate,
            savingsAccountEnabled = child.SavingsAccountEnabled,
            savingsBalance = showSavingsBalance ? child.SavingsBalance : (decimal?)null,
            savingsBalanceVisibleToChild = child.SavingsBalanceVisibleToChild,
            savingsTransferType = child.SavingsTransferType == SavingsTransferType.Percentage ? "Percentage" : "FixedAmount",
            savingsTransferPercentage = child.SavingsTransferPercentage,
            savingsTransferAmount = child.SavingsTransferAmount,
            createdAt = child.CreatedAt
        });
    }

    /// <summary>
    /// Get transactions for a child (iOS parity)
    /// </summary>
    [HttpGet("{childId}/transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetChildTransactions(
        Guid childId,
        [FromQuery] int limit = 20)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        // Verify user has access to this child
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

        var transactions = await _transactionService.GetChildTransactionsAsync(childId, limit);
        var transactionDtos = transactions.Select(TransactionDto.FromTransaction).ToList();

        return Ok(transactionDtos);
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
    /// Update all child settings including allowance, savings, and AllowanceDay (Parent only)
    /// </summary>
    [HttpPut("{childId}/settings")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<object>> UpdateChildSettings(Guid childId, [FromBody] UpdateChildSettingsDto dto)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var child = await _childManagementService.UpdateChildSettingsAsync(childId, dto, currentUser.Id);

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
            firstName = child.User.FirstName,
            lastName = child.User.LastName,
            weeklyAllowance = child.WeeklyAllowance,
            allowanceDay = child.AllowanceDay,
            savingsAccountEnabled = child.SavingsAccountEnabled,
            savingsBalanceVisibleToChild = child.SavingsBalanceVisibleToChild,
            savingsTransferType = child.SavingsTransferType.ToString(),
            savingsTransferPercentage = child.SavingsTransferPercentage,
            savingsTransferAmount = child.SavingsTransferAmount,
            message = "Child settings updated successfully"
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

    /// <summary>
    /// Pause a child's allowance (Parent only)
    /// </summary>
    [HttpPost("{childId}/allowance/pause")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult> PauseAllowance(Guid childId, [FromBody] PauseAllowanceDto dto)
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

        await _allowanceService.PauseAllowanceAsync(childId, dto.Reason);

        return Ok(new
        {
            childId,
            allowancePaused = true,
            reason = dto.Reason,
            message = "Allowance paused successfully"
        });
    }

    /// <summary>
    /// Resume a child's allowance (Parent only)
    /// </summary>
    [HttpPost("{childId}/allowance/resume")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult> ResumeAllowance(Guid childId)
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

        await _allowanceService.ResumeAllowanceAsync(childId);

        return Ok(new
        {
            childId,
            allowancePaused = false,
            message = "Allowance resumed successfully"
        });
    }

    /// <summary>
    /// Adjust a child's weekly allowance amount (Parent only)
    /// </summary>
    [HttpPut("{childId}/allowance/amount")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult> AdjustAllowanceAmount(Guid childId, [FromBody] AdjustAllowanceAmountDto dto)
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

        try
        {
            await _allowanceService.AdjustAllowanceAmountAsync(childId, dto.NewAmount, dto.Reason);

            return Ok(new
            {
                childId,
                oldAmount = child.WeeklyAllowance,
                newAmount = dto.NewAmount,
                reason = dto.Reason,
                message = "Allowance amount adjusted successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVALID_AMOUNT",
                    message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get allowance adjustment history for a child
    /// </summary>
    [HttpGet("{childId}/allowance/history")]
    public async Task<ActionResult<List<AllowanceAdjustmentDto>>> GetAllowanceHistory(Guid childId)
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

        var history = await _allowanceService.GetAllowanceAdjustmentHistoryAsync(childId);
        return Ok(history);
    }
}
