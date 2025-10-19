using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using AllowanceTracker.Serverless.Abstractions.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AllowanceTracker.Handlers;

/// <summary>
/// Cloud-agnostic children management handler
/// Contains business logic for all children endpoints
/// </summary>
public class ChildrenHandler
{
    private readonly IChildManagementService _childManagementService;
    private readonly IFamilyService _familyService;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<ChildrenHandler> _logger;

    public ChildrenHandler(
        IChildManagementService childManagementService,
        IFamilyService familyService,
        ITransactionService transactionService,
        ILogger<ChildrenHandler> logger)
    {
        _childManagementService = childManagementService;
        _familyService = familyService;
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all children in current user's family
    /// </summary>
    public async Task<IHttpResponse> GetChildrenAsync(IHttpContext httpContext, ClaimsPrincipal principal)
    {
        try
        {
            var userId = GetUserId(principal);
            var familyChildren = await _familyService.GetFamilyChildrenAsync(userId);

            if (familyChildren == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("NO_FAMILY", "Current user has no associated family");
            }

            var children = familyChildren.Children.Select(c => new ChildDto(
                c.ChildId,
                c.FirstName,
                c.LastName,
                c.WeeklyAllowance,
                c.CurrentBalance,
                c.LastAllowanceDate,
                c.AllowanceDay)).ToList();

            return await httpContext.CreateOkResponseAsync(children);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting children");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred retrieving children");
        }
    }

    /// <summary>
    /// Get child by ID
    /// </summary>
    public async Task<IHttpResponse> GetChildAsync(IHttpContext httpContext, ClaimsPrincipal principal, Guid childId)
    {
        try
        {
            var userId = GetUserId(principal);
            var child = await _childManagementService.GetChildAsync(childId, userId);

            if (child == null)
            {
                return await httpContext.CreateNotFoundResponseAsync("Child not found or access denied");
            }

            var nextAllowanceDate = child.LastAllowanceDate?.AddDays(7);

            return await httpContext.CreateOkResponseAsync(new
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
                savingsTransferType = child.SavingsTransferType == SavingsTransferType.Percentage ? "Percentage" : "FixedAmount",
                savingsTransferPercentage = child.SavingsTransferPercentage,
                savingsTransferAmount = child.SavingsTransferAmount,
                createdAt = child.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred retrieving child information");
        }
    }

    /// <summary>
    /// Get transactions for a child
    /// </summary>
    public async Task<IHttpResponse> GetChildTransactionsAsync(IHttpContext httpContext, ClaimsPrincipal principal, Guid childId, int limit = 20)
    {
        try
        {
            var userId = GetUserId(principal);
            var child = await _childManagementService.GetChildAsync(childId, userId);

            if (child == null)
            {
                return await httpContext.CreateNotFoundResponseAsync("Child not found or access denied");
            }

            var transactions = await _transactionService.GetChildTransactionsAsync(childId, limit);
            var transactionDtos = transactions.Select(TransactionDto.FromTransaction).ToList();

            return await httpContext.CreateOkResponseAsync(transactionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child transactions");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred retrieving transactions");
        }
    }

    /// <summary>
    /// Update child's weekly allowance (Parent only)
    /// </summary>
    public async Task<IHttpResponse> UpdateAllowanceAsync(IHttpContext httpContext, ClaimsPrincipal principal, Guid childId)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<UpdateAllowanceDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            var userId = GetUserId(principal);
            var child = await _childManagementService.UpdateChildAllowanceAsync(childId, dto.WeeklyAllowance, userId);

            if (child == null)
            {
                return await httpContext.CreateNotFoundResponseAsync("Child not found or access denied");
            }

            return await httpContext.CreateOkResponseAsync(new
            {
                childId = child.Id,
                weeklyAllowance = child.WeeklyAllowance,
                message = "Weekly allowance updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allowance");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred updating allowance");
        }
    }

    /// <summary>
    /// Update all child settings (Parent only)
    /// </summary>
    public async Task<IHttpResponse> UpdateChildSettingsAsync(IHttpContext httpContext, ClaimsPrincipal principal, Guid childId)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<UpdateChildSettingsDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            var userId = GetUserId(principal);
            var child = await _childManagementService.UpdateChildSettingsAsync(childId, dto, userId);

            if (child == null)
            {
                return await httpContext.CreateNotFoundResponseAsync("Child not found or access denied");
            }

            return await httpContext.CreateOkResponseAsync(new
            {
                childId = child.Id,
                firstName = child.User.FirstName,
                lastName = child.User.LastName,
                weeklyAllowance = child.WeeklyAllowance,
                allowanceDay = child.AllowanceDay,
                savingsAccountEnabled = child.SavingsAccountEnabled,
                savingsTransferType = child.SavingsTransferType.ToString(),
                savingsTransferPercentage = child.SavingsTransferPercentage,
                savingsTransferAmount = child.SavingsTransferAmount,
                message = "Child settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating child settings");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred updating settings");
        }
    }

    /// <summary>
    /// Delete child from family (Parent only)
    /// </summary>
    public async Task<IHttpResponse> DeleteChildAsync(IHttpContext httpContext, ClaimsPrincipal principal, Guid childId)
    {
        try
        {
            var userId = GetUserId(principal);
            var deleted = await _childManagementService.DeleteChildAsync(childId, userId);

            if (!deleted)
            {
                return await httpContext.CreateNotFoundResponseAsync("Child not found or access denied");
            }

            return httpContext.CreateNoContentResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting child");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred deleting child");
        }
    }

    /// <summary>
    /// Extract user ID from claims principal
    /// </summary>
    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }
}
