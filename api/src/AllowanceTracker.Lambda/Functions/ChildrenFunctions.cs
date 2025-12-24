using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.DependencyInjection;
using AllowanceTracker.AWS.Adapter.Http;
using AllowanceTracker.Handlers;
using AllowanceTracker.Helpers;

namespace AllowanceTracker.Lambda.Functions;

/// <summary>
/// Children management Lambda functions
/// AWS Lambda wrapper around cloud-agnostic ChildrenHandler
/// </summary>
public class ChildrenFunctions
{
    private readonly ChildrenHandler _childrenHandler;
    private readonly AuthorizationHelper _authHelper;

    public ChildrenFunctions()
    {
        _childrenHandler = Startup.ServiceProvider.GetRequiredService<ChildrenHandler>();
        _authHelper = Startup.ServiceProvider.GetRequiredService<AuthorizationHelper>();
    }

    /// <summary>
    /// Get all children in current user's family
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> GetChildren(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var response = await _childrenHandler.GetChildrenAsync(httpContext, principal);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Get child by ID
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> GetChild(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((LambdaHttpResponse)badRequest).BuildLambdaResponse();
        }

        var response = await _childrenHandler.GetChildAsync(httpContext, principal, childId.Value);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Get transactions for a child
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> GetChildTransactions(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((LambdaHttpResponse)badRequest).BuildLambdaResponse();
        }

        var limit = int.TryParse(httpContext.Request.GetQueryParameter("limit"), out var l) ? l : 20;

        var response = await _childrenHandler.GetChildTransactionsAsync(httpContext, principal, childId.Value, limit);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Update child's weekly allowance (Parent only)
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> UpdateAllowance(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((LambdaHttpResponse)badRequest).BuildLambdaResponse();
        }

        var response = await _childrenHandler.UpdateAllowanceAsync(httpContext, principal, childId.Value);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Update all child settings (Parent only)
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> UpdateChildSettings(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((LambdaHttpResponse)badRequest).BuildLambdaResponse();
        }

        var response = await _childrenHandler.UpdateChildSettingsAsync(httpContext, principal, childId.Value);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Delete child from family (Parent only)
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> DeleteChild(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var childId = httpContext.Request.GetRouteGuid("childId");
        if (childId == null)
        {
            var badRequest = await httpContext.CreateBadRequestResponseAsync("INVALID_CHILD_ID", "Invalid child ID format");
            return ((LambdaHttpResponse)badRequest).BuildLambdaResponse();
        }

        var response = await _childrenHandler.DeleteChildAsync(httpContext, principal, childId.Value);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }
}
