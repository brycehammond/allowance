using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

/// <summary>
/// Manages financial transactions for children's allowance accounts.
/// Note: GET operations for transactions and balance are available via ChildrenController
/// at /api/v1/children/{childId}/transactions and /api/v1/children/{childId}/balance
/// </summary>
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

    /// <summary>
    /// Create a new transaction (credit, debit, or allowance)
    /// </summary>
    /// <param name="dto">Transaction details including amount, type, category, and description</param>
    /// <returns>The created transaction with updated balance</returns>
    /// <response code="201">Transaction created successfully</response>
    /// <response code="400">Invalid transaction data (e.g., insufficient funds, negative amount)</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User does not have permission to create transactions for this child</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/transactions
    ///     {
    ///         "childId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "amount": 25.50,
    ///         "type": "Credit",
    ///         "category": "Chore",
    ///         "description": "Cleaned room and did dishes"
    ///     }
    ///
    /// Transaction types:
    /// - Credit: Adds money to the child's balance
    /// - Debit: Subtracts money from the child's balance
    /// - Allowance: Weekly allowance payment (typically automated)
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        return CreatedAtAction(
            actionName: "GetChildTransactions",
            controllerName: "Children",
            routeValues: new { childId = dto.ChildId },
            value: transactionDto);
    }
}
