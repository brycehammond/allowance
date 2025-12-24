using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AllowanceTracker.Azure.Adapter.Http;
using AllowanceTracker.Handlers;
using AllowanceTracker.Helpers;

namespace AllowanceTracker.Functions.Functions.Http;

/// <summary>
/// Authentication and registration functions for the allowance tracker system
/// Azure Functions wrapper around cloud-agnostic AuthHandler
/// </summary>
public class AuthFunctions
{
    private readonly AuthHandler _authHandler;
    private readonly AuthorizationHelper _authHelper;
    private readonly ILogger<AuthFunctions> _logger;

    public AuthFunctions(
        AuthHandler authHandler,
        AuthorizationHelper authHelper,
        ILogger<AuthFunctions> logger)
    {
        _authHandler = authHandler;
        _authHelper = authHelper;
        _logger = logger;
    }

    /// <summary>
    /// Register a new parent account with family
    /// </summary>
    [Function("RegisterParent")]
    public async Task<HttpResponseData> RegisterParent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/v1/auth/register/parent")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var response = await _authHandler.RegisterParentAsync(httpContext);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Register a new child account (Parent only)
    /// </summary>
    [Function("RegisterChild")]
    public async Task<HttpResponseData> RegisterChild(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/v1/auth/register/child")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var response = await _authHandler.RegisterChildAsync(httpContext, principal);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Add an additional parent to the family (Parent only)
    /// </summary>
    [Function("RegisterAdditionalParent")]
    public async Task<HttpResponseData> RegisterAdditionalParent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/v1/auth/register/parent/additional")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext, new[] { "Parent" });

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var response = await _authHandler.RegisterAdditionalParentAsync(httpContext, principal);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [Function("Login")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/v1/auth/login")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var response = await _authHandler.LoginAsync(httpContext);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [Function("GetCurrentUser")]
    public async Task<HttpResponseData> GetCurrentUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/v1/auth/me")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var response = await _authHandler.GetCurrentUserAsync(httpContext, principal);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [Function("ChangePassword")]
    public async Task<HttpResponseData> ChangePassword(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/v1/auth/change-password")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var (isAuthorized, principal, errorResponse) = await _authHelper.CheckAuthorizationAsync(httpContext);

        if (!isAuthorized || principal == null)
            return ((AzureFunctionsHttpResponse)errorResponse!).GetUnderlyingResponse();

        var response = await _authHandler.ChangePasswordAsync(httpContext, principal);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    [Function("ForgotPassword")]
    public async Task<HttpResponseData> ForgotPassword(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/v1/auth/forgot-password")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var response = await _authHandler.ForgotPasswordAsync(httpContext);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }

    /// <summary>
    /// Reset password using reset token from email
    /// </summary>
    [Function("ResetPassword")]
    public async Task<HttpResponseData> ResetPassword(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/v1/auth/reset-password")]
        HttpRequestData req)
    {
        var httpContext = new AzureFunctionsHttpContext(req);
        var response = await _authHandler.ResetPasswordAsync(httpContext);
        return ((AzureFunctionsHttpResponse)response).GetUnderlyingResponse();
    }
}
