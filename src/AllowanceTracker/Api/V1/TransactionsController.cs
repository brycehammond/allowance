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
    public async Task<ActionResult<List<TransactionDto>>> GetChildTransactions(
        Guid childId,
        [FromQuery] int limit = 20)
    {
        var transactions = await _transactionService.GetChildTransactionsAsync(childId, limit);
        var dtos = transactions.Select(TransactionDto.FromTransaction).ToList();
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionDto dto)
    {
        var transaction = await _transactionService.CreateTransactionAsync(dto);

        // Reload to get CreatedBy navigation property
        var transactions = await _transactionService.GetChildTransactionsAsync(dto.ChildId, 1);
        var latestTransaction = transactions.FirstOrDefault();

        if (latestTransaction == null)
        {
            return StatusCode(500, "Transaction was created but could not be retrieved");
        }

        var transactionDto = TransactionDto.FromTransaction(latestTransaction);
        return CreatedAtAction(nameof(GetChildTransactions),
            new { childId = dto.ChildId },
            transactionDto);
    }

    [HttpGet("children/{childId}/balance")]
    public async Task<ActionResult<decimal>> GetBalance(Guid childId)
    {
        var balance = await _transactionService.GetCurrentBalanceAsync(childId);
        return Ok(new { balance });
    }
}
