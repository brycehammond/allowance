using System.Security.Claims;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Http;

namespace AllowanceTracker.Functions.Services;

/// <summary>
/// A CurrentUserService implementation for Azure Functions that falls back to a system user
/// when there is no HTTP context (e.g., timer-triggered functions).
/// </summary>
public class FunctionCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Well-known system user ID for automated/scheduled operations.
    /// This user must exist in the AspNetUsers table.
    /// </summary>
    public static readonly Guid SystemUserId = Data.Constants.SystemUserId;

    public FunctionCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            // Try to get user from HTTP context (for HTTP-triggered functions)
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Fall back to system user for timer-triggered functions
            return SystemUserId;
        }
    }

    public string Email
    {
        get
        {
            var email = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
            return email ?? "system@allowancetracker.local";
        }
    }
}
