using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("children/{childId}")]
    public async Task<ActionResult<List<Transaction>>> GetChildTransactions(
        Guid childId,
        [FromQuery] int limit = 20)
    {
        var transactions = await _transactionService.GetChildTransactionsAsync(childId, limit);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<Transaction>> CreateTransaction(CreateTransactionDto dto)
    {
        var transaction = await _transactionService.CreateTransactionAsync(dto);
        return CreatedAtAction(nameof(GetChildTransactions),
            new { childId = dto.ChildId },
            transaction);
    }

    [HttpGet("children/{childId}/balance")]
    public async Task<ActionResult<decimal>> GetBalance(Guid childId)
    {
        var balance = await _transactionService.GetCurrentBalanceAsync(childId);
        return Ok(new { balance });
    }
}
