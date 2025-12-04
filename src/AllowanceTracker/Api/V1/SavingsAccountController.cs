using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/savings")]
[Authorize]
public class SavingsAccountController : ControllerBase
{
    private readonly ISavingsAccountService _savingsAccountService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAccountService _accountService;
    private readonly IChildManagementService _childManagementService;

    public SavingsAccountController(
        ISavingsAccountService savingsAccountService,
        ICurrentUserService currentUserService,
        IAccountService accountService,
        IChildManagementService childManagementService)
    {
        _savingsAccountService = savingsAccountService;
        _currentUserService = currentUserService;
        _accountService = accountService;
        _childManagementService = childManagementService;
    }

    /// <summary>
    /// Enable savings account for a child
    /// </summary>
    [HttpPost("enable")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> EnableSavingsAccount([FromBody] EnableSavingsAccountRequest request)
    {
        await _savingsAccountService.EnableSavingsAccountAsync(
            request.ChildId,
            request.TransferType,
            request.Amount);

        return Ok(new { Message = "Savings account enabled successfully" });
    }

    /// <summary>
    /// Update savings account configuration
    /// </summary>
    [HttpPut("config")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> UpdateSavingsConfig([FromBody] UpdateSavingsConfigRequest request)
    {
        await _savingsAccountService.UpdateSavingsConfigAsync(
            request.ChildId,
            request.TransferType,
            request.Amount);

        return Ok(new { Message = "Savings configuration updated successfully" });
    }

    /// <summary>
    /// Disable savings account for a child
    /// </summary>
    [HttpPost("{childId}/disable")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> DisableSavingsAccount(Guid childId)
    {
        await _savingsAccountService.DisableSavingsAccountAsync(childId);

        return Ok(new { Message = "Savings account disabled successfully" });
    }

    /// <summary>
    /// Manually deposit to savings from main balance
    /// </summary>
    [HttpPost("deposit")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<SavingsTransaction>> DepositToSavings([FromBody] DepositToSavingsRequest request)
    {
        var transaction = await _savingsAccountService.DepositToSavingsAsync(
            request.ChildId,
            request.Amount,
            request.Description,
            _currentUserService.UserId);

        return CreatedAtAction(
            nameof(GetSavingsHistory),
            new { childId = request.ChildId },
            transaction);
    }

    /// <summary>
    /// Withdraw from savings to main balance
    /// </summary>
    [HttpPost("withdraw")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<SavingsTransaction>> WithdrawFromSavings([FromBody] WithdrawFromSavingsRequest request)
    {
        var transaction = await _savingsAccountService.WithdrawFromSavingsAsync(
            request.ChildId,
            request.Amount,
            request.Description,
            _currentUserService.UserId);

        return CreatedAtAction(
            nameof(GetSavingsHistory),
            new { childId = request.ChildId },
            transaction);
    }

    /// <summary>
    /// Get current savings balance for a child
    /// </summary>
    [HttpGet("{childId}/balance")]
    public async Task<ActionResult<object>> GetSavingsBalance(Guid childId)
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

        // Determine if savings balance should be shown
        var showSavingsBalance = currentUser.Role == UserRole.Parent || child.SavingsBalanceVisibleToChild;

        if (!showSavingsBalance)
        {
            return Ok(new { balance = (decimal?)null });
        }

        var balance = await _savingsAccountService.GetSavingsBalanceAsync(childId);
        return Ok(new { balance });
    }

    /// <summary>
    /// Get savings transaction history for a child
    /// </summary>
    [HttpGet("{childId}/history")]
    public async Task<ActionResult<List<SavingsTransaction>>> GetSavingsHistory(Guid childId, [FromQuery] int limit = 50)
    {
        var transactions = await _savingsAccountService.GetSavingsHistoryAsync(childId, limit);
        return Ok(transactions);
    }

    /// <summary>
    /// Get comprehensive savings account summary
    /// </summary>
    [HttpGet("{childId}/summary")]
    public async Task<ActionResult<object>> GetSavingsSummary(Guid childId)
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

        var summary = await _savingsAccountService.GetSummaryAsync(childId);

        // Determine if savings balance should be shown
        var showSavingsBalance = currentUser.Role == UserRole.Parent || child.SavingsBalanceVisibleToChild;

        if (!showSavingsBalance)
        {
            // Return summary with hidden balance information
            return Ok(new
            {
                childId = summary.ChildId,
                isEnabled = summary.IsEnabled,
                currentBalance = (decimal?)null,
                transferType = summary.TransferType,
                transferAmount = summary.TransferAmount,
                transferPercentage = summary.TransferPercentage,
                totalTransactions = (int?)null,
                totalDeposited = (decimal?)null,
                totalWithdrawn = (decimal?)null,
                lastTransactionDate = (DateTime?)null,
                configDescription = summary.ConfigDescription,
                balanceHidden = true
            });
        }

        return Ok(summary);
    }
}
