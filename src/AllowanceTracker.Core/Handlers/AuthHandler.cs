using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Services;
using AllowanceTracker.Serverless.Abstractions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AllowanceTracker.Handlers;

/// <summary>
/// Cloud-agnostic authentication handler
/// Contains business logic for all authentication endpoints
/// </summary>
public class AuthHandler
{
    private readonly IAccountService _accountService;
    private readonly IJwtService _jwtService;
    private readonly AllowanceContext _context;
    private readonly ILogger<AuthHandler> _logger;

    public AuthHandler(
        IAccountService accountService,
        IJwtService jwtService,
        AllowanceContext context,
        ILogger<AuthHandler> logger)
    {
        _accountService = accountService;
        _jwtService = jwtService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Register a new parent account with family
    /// </summary>
    public async Task<IHttpResponse> RegisterParentAsync(IHttpContext httpContext)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<RegisterParentDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            var result = await _accountService.RegisterParentAsync(dto);

            if (!result.Succeeded)
            {
                return await httpContext.CreateBadRequestResponseAsync(
                    "REGISTRATION_FAILED",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Auto-login
            var loginResult = await _accountService.LoginAsync(dto.Email, dto.Password);
            if (!loginResult.Succeeded)
            {
                return await httpContext.CreateServerErrorResponseAsync(
                    "Account created but auto-login failed. Please login manually.");
            }

            var user = await _context.Users
                .Include(u => u.Family)
                .FirstAsync(u => u.Email == dto.Email);

            var token = _jwtService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddDays(1);

            return await httpContext.CreateCreatedResponseAsync(new AuthResponseDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.Role.ToString(),
                user.FamilyId,
                user.Family?.Name,
                token,
                expiresAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering parent");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred during registration");
        }
    }

    /// <summary>
    /// Register a new child account (Parent only)
    /// </summary>
    public async Task<IHttpResponse> RegisterChildAsync(IHttpContext httpContext, ClaimsPrincipal principal)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<RegisterChildDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            var currentUserId = GetUserId(principal);
            var currentUser = await _context.Users.FindAsync(currentUserId);

            if (currentUser?.FamilyId == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("NO_FAMILY", "Current user has no associated family");
            }

            var result = await _accountService.RegisterChildAsync(dto, currentUser.FamilyId.Value);

            if (!result.Succeeded)
            {
                return await httpContext.CreateBadRequestResponseAsync(
                    "REGISTRATION_FAILED",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var user = await _context.Users
                .Include(u => u.Family)
                .Include(u => u.ChildProfile)
                .FirstAsync(u => u.Email == dto.Email);

            return await httpContext.CreateCreatedResponseAsync(new
            {
                userId = user.Id,
                childId = user.ChildProfile!.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                role = user.Role.ToString(),
                familyId = user.FamilyId,
                weeklyAllowance = user.ChildProfile.WeeklyAllowance,
                currentBalance = user.ChildProfile.CurrentBalance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering child");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred during registration");
        }
    }

    /// <summary>
    /// Add an additional parent to the family (Parent only)
    /// </summary>
    public async Task<IHttpResponse> RegisterAdditionalParentAsync(IHttpContext httpContext, ClaimsPrincipal principal)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<RegisterAdditionalParentDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            var currentUserId = GetUserId(principal);
            var currentUser = await _context.Users.FindAsync(currentUserId);

            if (currentUser?.FamilyId == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("NO_FAMILY", "Current user has no associated family");
            }

            var result = await _accountService.RegisterAdditionalParentAsync(dto, currentUser.FamilyId.Value);

            if (!result.Succeeded)
            {
                return await httpContext.CreateBadRequestResponseAsync(
                    "REGISTRATION_FAILED",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var user = await _context.Users
                .Include(u => u.Family)
                .FirstAsync(u => u.Email == dto.Email);

            return await httpContext.CreateCreatedResponseAsync(new UserInfoDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.Role.ToString(),
                user.FamilyId,
                user.Family?.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering additional parent");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred during registration");
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    public async Task<IHttpResponse> LoginAsync(IHttpContext httpContext)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<LoginDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            var result = await _accountService.LoginAsync(dto.Email, dto.Password, dto.RememberMe);

            if (!result.Succeeded)
            {
                return await httpContext.CreateUnauthorizedResponseAsync("Invalid email or password");
            }

            var user = await _context.Users
                .Include(u => u.Family)
                .FirstAsync(u => u.Email == dto.Email);

            var token = _jwtService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddDays(1);

            return await httpContext.CreateOkResponseAsync(new AuthResponseDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.Role.ToString(),
                user.FamilyId,
                user.Family?.Name,
                token,
                expiresAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred during login");
        }
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    public async Task<IHttpResponse> GetCurrentUserAsync(IHttpContext httpContext, ClaimsPrincipal principal)
    {
        try
        {
            var userId = GetUserId(principal);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return await httpContext.CreateUnauthorizedResponseAsync("User not found");
            }

            var family = user.FamilyId.HasValue
                ? await _context.Families.FindAsync(user.FamilyId.Value)
                : null;

            return await httpContext.CreateOkResponseAsync(new UserInfoDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.Role.ToString(),
                user.FamilyId,
                family?.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred retrieving user information");
        }
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    public async Task<IHttpResponse> ChangePasswordAsync(IHttpContext httpContext, ClaimsPrincipal principal)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<ChangePasswordDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            var userId = GetUserId(principal);
            var result = await _accountService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                return await httpContext.CreateBadRequestResponseAsync(
                    "PASSWORD_CHANGE_FAILED",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return await httpContext.CreateOkResponseAsync(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred during password change");
        }
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    public async Task<IHttpResponse> ForgotPasswordAsync(IHttpContext httpContext)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<ForgotPasswordDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            await _accountService.ForgotPasswordAsync(dto.Email);

            // Always return success for security (don't reveal if email exists)
            return await httpContext.CreateOkResponseAsync(new
            {
                message = "If your email is registered, you will receive a password reset link shortly"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred processing your request");
        }
    }

    /// <summary>
    /// Reset password using reset token from email
    /// </summary>
    public async Task<IHttpResponse> ResetPasswordAsync(IHttpContext httpContext)
    {
        try
        {
            var dto = await httpContext.Request.ReadFromJsonAsync<ResetPasswordDto>();
            if (dto == null)
            {
                return await httpContext.CreateBadRequestResponseAsync("INVALID_REQUEST", "Request body is required");
            }

            var result = await _accountService.ResetPasswordAsync(dto.Email, dto.ResetToken, dto.NewPassword);

            if (!result.Succeeded)
            {
                return await httpContext.CreateBadRequestResponseAsync(
                    "PASSWORD_RESET_FAILED",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return await httpContext.CreateOkResponseAsync(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return await httpContext.CreateServerErrorResponseAsync("An error occurred during password reset");
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
