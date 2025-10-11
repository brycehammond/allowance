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

    public SavingsAccountController(
        ISavingsAccountService savingsAccountService,
        ICurrentUserService currentUserService)
    {
        _savingsAccountService = savingsAccountService;
        _currentUserService = currentUserService;
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
    public async Task<ActionResult<decimal>> GetSavingsBalance(Guid childId)
    {
        var balance = await _savingsAccountService.GetSavingsBalanceAsync(childId);
        return Ok(balance);
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
    public async Task<ActionResult<SavingsAccountSummary>> GetSavingsSummary(Guid childId)
    {
        var summary = await _savingsAccountService.GetSummaryAsync(childId);
        return Ok(summary);
    }
}
