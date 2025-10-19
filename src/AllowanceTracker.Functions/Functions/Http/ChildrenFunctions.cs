using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AllowanceTracker.Azure.Adapter.Http;
using AllowanceTracker.Handlers;
using AllowanceTracker.Helpers;

namespace AllowanceTracker.Functions.Functions.Http;

/// <summary>
/// Children management functions
/// Azure Functions wrapper around cloud-agnostic ChildrenHandler
/// </summary>
public class ChildrenFunctions
{
    private readonly ChildrenHandler _childrenHandler;
    private readonly AuthorizationHelper _authHelper;
    private readonly ILogger<ChildrenFunctions> _logger;

    public ChildrenFunctions(
        ChildrenHandler childrenHandler,
        AuthorizationHelper authHelper,
        ILogger<ChildrenFunctions> logger)
    {
        _childrenHandler = childrenHandler;
        _authHelper = authHelper;
        _logger = logger;
    }

    /// <summary>
    /// Get all children in current user's family
    /// </summary>
    [Function("GetChildren")]
    public async Task<HttpResponseData> GetChildren(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/v1/children")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var response = await _childrenHandler.GetChildrenAsync(httpContext, principal);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Get child by ID
    /// </summary>
    [Function("GetChild")]
    public async Task<HttpResponseData> GetChild(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/v1/children/{childId}")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((AzureFunctionsHttpResponse)badRequest).GetUnderlyingResponse();
        }

        var response = await _childrenHandler.GetChildAsync(httpContext, principal, childId.Value);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Get transactions for a child
    /// </summary>
    [Function("GetChildTransactions")]
    public async Task<HttpResponseData> GetChildTransactions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/v1/children/{childId}/transactions")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((AzureFunctionsHttpResponse)badRequest).GetUnderlyingResponse();
        }

        var limit = int.TryParse(httpContext.Request.GetQueryParameter("limit"), out var l) ? l : 20;

        var response = await _childrenHandler.GetChildTransactionsAsync(httpContext, principal, childId.Value, limit);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Update child's weekly allowance (Parent only)
    /// </summary>
    [Function("UpdateAllowance")]
    public async Task<HttpResponseData> UpdateAllowance(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "api/v1/children/{childId}/allowance")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((AzureFunctionsHttpResponse)badRequest).GetUnderlyingResponse();
        }

        var response = await _childrenHandler.UpdateAllowanceAsync(httpContext, principal, childId.Value);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Update all child settings (Parent only)
    /// </summary>
    [Function("UpdateChildSettings")]
    public async Task<HttpResponseData> UpdateChildSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "api/v1/children/{childId}/settings")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((AzureFunctionsHttpResponse)badRequest).GetUnderlyingResponse();
        }

        var response = await _childrenHandler.UpdateChildSettingsAsync(httpContext, principal, childId.Value);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Delete child from family (Parent only)
    /// </summary>
    [Function("DeleteChild")]
    public async Task<HttpResponseData> DeleteChild(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "api/v1/children/{childId}")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((AzureFunctionsHttpResponse)badRequest).GetUnderlyingResponse();
        }

        var response = await _childrenHandler.DeleteChildAsync(httpContext, principal, childId.Value);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }
}
