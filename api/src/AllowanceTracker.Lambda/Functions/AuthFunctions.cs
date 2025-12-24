using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.DependencyInjection;
using AllowanceTracker.AWS.Adapter.Http;
using AllowanceTracker.Handlers;
using AllowanceTracker.Helpers;

// Assembly attribute to enable Lambda JSON serialization
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AllowanceTracker.Lambda.Functions;

/// <summary>
/// Authentication and registration Lambda functions
/// AWS Lambda wrapper around cloud-agnostic AuthHandler
/// </summary>
public class AuthFunctions
{
    private readonly AuthHandler _authHandler;
    private readonly AuthorizationHelper _authHelper;

    public AuthFunctions()
    {
        _authHandler = Startup.ServiceProvider.GetRequiredService<AuthHandler>();
        _authHelper = Startup.ServiceProvider.GetRequiredService<AuthorizationHelper>();
    }

    /// <summary>
    /// Register a new parent account with family
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> RegisterParent(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var response = await _authHandler.RegisterParentAsync(httpContext);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Register a new child account (Parent only)
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> RegisterChild(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var response = await _authHandler.RegisterChildAsync(httpContext, principal);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Add an additional parent to the family (Parent only)
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> RegisterAdditionalParent(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var response = await _authHandler.RegisterAdditionalParentAsync(httpContext, principal);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> Login(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var response = await _authHandler.LoginAsync(httpContext);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> GetCurrentUser(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var response = await _authHandler.GetCurrentUserAsync(httpContext, principal);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> ChangePassword(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((LambdaHttpResponse)errorResponse!).BuildLambdaResponse();

        var response = await _authHandler.ChangePasswordAsync(httpContext, principal);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> ForgotPassword(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var response = await _authHandler.ForgotPasswordAsync(httpContext);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }

    /// <summary>
    /// Reset password using reset token from email
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> ResetPassword(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var httpContext = new LambdaHttpContext(request);
        var response = await _authHandler.ResetPasswordAsync(httpContext);
        return ((LambdaHttpResponse)response).BuildLambdaResponse();
    }
}
