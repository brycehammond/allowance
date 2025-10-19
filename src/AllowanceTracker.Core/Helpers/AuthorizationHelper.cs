using AllowanceTracker.Serverless.Abstractions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AllowanceTracker.Helpers;

/// <summary>
/// Cloud-agnostic JWT authorization helper
/// Works with IHttpContext abstraction instead of cloud-specific request types
/// </summary>
public class AuthorizationHelper
{
    private readonly IConfiguration _configuration;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public AuthorizationHelper(IConfiguration configuration)
    {
        _configuration = configuration;

        var jwtSecret = _configuration["Jwt:SecretKey"]
            ?? _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured");

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Validate JWT token from Authorization header
    /// </summary>
    public ClaimsPrincipal? ValidateToken(IHttpRequest request)
    {
        try
        {
            var authHeader = request.GetHeader("Authorization");
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if request is authorized (has valid JWT)
    /// Returns tuple with authorization status, principal, and error response
    /// </summary>
    public async Task<(bool IsAuthorized, ClaimsPrincipal? Principal, IHttpResponse? ErrorResponse)> CheckAuthorizationAsync(
        IHttpContext httpContext,
        string[]? requiredRoles = null)
    {
        var principal = ValidateToken(httpContext.Request);

        if (principal == null)
        {
            var response = await httpContext.CreateUnauthorizedResponseAsync("Valid JWT token required");
            return (false, null, response);
        }

        // Check roles if specified
        if (requiredRoles != null && requiredRoles.Length > 0)
        {
            var userRole = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == null || !requiredRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                var response = await httpContext.CreateForbiddenResponseAsync($"User must have one of the following roles: {string.Join(", ", requiredRoles)}");
                return (false, principal, response);
            }
        }

        return (true, principal, null);
    }

    /// <summary>
    /// Get user ID from claims
    /// </summary>
    public static Guid GetUserId(ClaimsPrincipal principal)
    {
        var nameIdentifier = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID not found in token");
        return Guid.Parse(nameIdentifier);
    }

    /// <summary>
    /// Get family ID from claims
    /// </summary>
    public static Guid? GetFamilyId(ClaimsPrincipal principal)
    {
        var familyId = principal.FindFirst("FamilyId")?.Value;
        return familyId != null ? Guid.Parse(familyId) : null;
    }

    /// <summary>
    /// Get user role from claims
    /// </summary>
    public static string? GetRole(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Check if user is a parent
    /// </summary>
    public static bool IsParent(ClaimsPrincipal principal)
    {
        var role = GetRole(principal);
        return role?.Equals("Parent", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Check if user is a child
    /// </summary>
    public static bool IsChild(ClaimsPrincipal principal)
    {
        var role = GetRole(principal);
        return role?.Equals("Child", StringComparison.OrdinalIgnoreCase) == true;
    }
}
