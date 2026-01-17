using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Api.V1;

/// <summary>
/// Handles user authentication and registration for the allowance tracker system
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IJwtService _jwtService;
    private readonly AllowanceContext _context;

    public AuthController(
        IAccountService accountService,
        IJwtService jwtService,
        AllowanceContext context)
    {
        _accountService = accountService;
        _jwtService = jwtService;
        _context = context;
    }

    /// <summary>
    /// Register a new parent account with family
    /// </summary>
    /// <param name="dto">Parent registration details including email, password, name, and family name</param>
    /// <returns>Authentication response with JWT token</returns>
    /// <response code="201">Parent account and family created successfully, user is automatically logged in</response>
    /// <response code="400">Registration failed (e.g., email already exists, invalid password)</response>
    /// <response code="500">Account created but auto-login failed</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/auth/register/parent
    ///     {
    ///         "email": "parent@example.com",
    ///         "password": "Password123",
    ///         "firstName": "John",
    ///         "lastName": "Doe",
    ///         "familyName": "Doe Family"
    ///     }
    ///
    /// Password requirements:
    /// - Minimum 6 characters
    /// - At least one digit required
    ///
    /// Upon successful registration:
    /// - A new family is created with the specified name
    /// - The parent account is created and assigned to the family
    /// - The user is automatically logged in and receives a JWT token
    /// </remarks>
    [HttpPost("register/parent")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> RegisterParent([FromBody] RegisterParentDto dto)
    {
        var result = await _accountService.RegisterParentAsync(dto);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "REGISTRATION_FAILED",
                    message = string.Join(", ", result.Errors.Select(e => e.Description))
                }
            });
        }

        // Auto-login
        var loginResult = await _accountService.LoginAsync(dto.Email, dto.Password);
        if (!loginResult.Succeeded)
        {
            return StatusCode(500, new
            {
                error = new
                {
                    code = "AUTO_LOGIN_FAILED",
                    message = "Account created but auto-login failed. Please login manually."
                }
            });
        }

        var user = await _context.Users
            .Include(u => u.Family)
            .FirstAsync(u => u.Email == dto.Email);

        var token = _jwtService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddDays(1);

        return CreatedAtAction(
            nameof(GetCurrentUser),
            new AuthResponseDto(
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

    /// <summary>
    /// Register a new child account (Parent only)
    /// </summary>
    /// <param name="dto">Child registration details including email, password, name, and allowance settings</param>
    /// <returns>User information for the newly created child account</returns>
    /// <response code="201">Child account created successfully</response>
    /// <response code="400">Registration failed (e.g., email already exists, parent has no family)</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not a parent</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/auth/register/child
    ///     {
    ///         "email": "child@example.com",
    ///         "password": "Password123",
    ///         "firstName": "Emma",
    ///         "lastName": "Doe",
    ///         "weeklyAllowance": 10.00
    ///     }
    ///
    /// Requirements:
    /// - Only parents can create child accounts
    /// - Child is automatically added to the parent's family
    /// - Child starts with zero balance
    /// </remarks>
    [HttpPost("register/child")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserInfoDto>> RegisterChild([FromBody] RegisterChildDto dto)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser?.FamilyId == null)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "NO_FAMILY",
                    message = "Current user has no associated family"
                }
            });
        }

        var result = await _accountService.RegisterChildAsync(dto, currentUser.FamilyId.Value);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "REGISTRATION_FAILED",
                    message = string.Join(", ", result.Errors.Select(e => e.Description))
                }
            });
        }

        var user = await _context.Users
            .Include(u => u.Family)
            .Include(u => u.ChildProfile)
            .FirstAsync(u => u.Email == dto.Email);

        return CreatedAtAction(
            nameof(GetCurrentUser),
            new
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

    /// <summary>
    /// Add an additional parent to the family (Parent only)
    /// </summary>
    /// <param name="dto">Parent registration details including email, password, and name</param>
    /// <returns>User information for the newly created parent account</returns>
    /// <response code="201">Parent account created and added to family successfully</response>
    /// <response code="400">Registration failed (e.g., email already exists, current user has no family)</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User is not a parent</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/auth/register/parent/additional
    ///     {
    ///         "email": "parent2@example.com",
    ///         "password": "Password123",
    ///         "firstName": "Jane",
    ///         "lastName": "Doe"
    ///     }
    ///
    /// Requirements:
    /// - Only parents can add additional parents
    /// - New parent is automatically added to the current parent's family
    /// - Useful for adding co-parents or guardians to manage the family together
    /// </remarks>
    [HttpPost("register/parent/additional")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserInfoDto>> RegisterAdditionalParent([FromBody] RegisterAdditionalParentDto dto)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser?.FamilyId == null)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "NO_FAMILY",
                    message = "Current user has no associated family"
                }
            });
        }

        var result = await _accountService.RegisterAdditionalParentAsync(dto, currentUser.FamilyId.Value);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "REGISTRATION_FAILED",
                    message = string.Join(", ", result.Errors.Select(e => e.Description))
                }
            });
        }

        var user = await _context.Users
            .Include(u => u.Family)
            .FirstAsync(u => u.Email == dto.Email);

        return CreatedAtAction(
            nameof(GetCurrentUser),
            new UserInfoDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.Role.ToString(),
                user.FamilyId,
                user.Family?.Name));
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="dto">Login credentials (email and password)</param>
    /// <returns>Authentication response with JWT token valid for 24 hours</returns>
    /// <response code="200">Login successful, returns user info and JWT token</response>
    /// <response code="401">Invalid credentials</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/v1/auth/login
    ///     {
    ///         "email": "parent@example.com",
    ///         "password": "Password123",
    ///         "rememberMe": false
    ///     }
    ///
    /// The returned JWT token should be included in the Authorization header for all subsequent requests:
    ///
    ///     Authorization: Bearer {token}
    ///
    /// Token expiration: 24 hours from login
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var result = await _accountService.LoginAsync(dto.Email, dto.Password, dto.RememberMe);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                error = new
                {
                    code = "INVALID_CREDENTIALS",
                    message = "Invalid email or password"
                }
            });
        }

        var user = await _context.Users
            .Include(u => u.Family)
            .Include(u => u.ChildProfile)
            .FirstAsync(u => u.Email == dto.Email);

        // Get childId for child users
        Guid? childId = user.Role == UserRole.Child ? user.ChildProfile?.Id : null;

        var token = _jwtService.GenerateToken(user, childId);
        var expiresAt = DateTime.UtcNow.AddDays(1);

        return Ok(new AuthResponseDto(
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

    /// <summary>
    /// Refresh the current JWT token
    /// </summary>
    /// <returns>New authentication response with a fresh JWT token</returns>
    /// <response code="200">Token refreshed successfully, returns new token valid for 24 hours</response>
    /// <response code="401">Current token is invalid or expired</response>
    /// <remarks>
    /// Call this endpoint before the current token expires to maintain the session.
    /// The new token will be valid for 24 hours from the time of refresh.
    ///
    /// Recommended usage:
    /// - Check token expiration on app launch or before API calls
    /// - If token expires within the next hour, call this endpoint to get a fresh token
    /// - Store the new token and expiration time
    ///
    /// This is useful for:
    /// - Keeping users logged in without requiring re-authentication
    /// - Implementing "remember me" functionality with biometric unlock
    /// </remarks>
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken()
    {
        var user = await _accountService.GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(new
            {
                error = new
                {
                    code = "INVALID_TOKEN",
                    message = "Token is invalid or expired"
                }
            });
        }

        var family = user.FamilyId.HasValue
            ? await _context.Families.FindAsync(user.FamilyId.Value)
            : null;

        // Get childId for child users
        Guid? childId = null;
        if (user.Role == UserRole.Child)
        {
            var child = await _context.Children.FirstOrDefaultAsync(c => c.UserId == user.Id);
            childId = child?.Id;
        }

        var token = _jwtService.GenerateToken(user, childId);
        var expiresAt = DateTime.UtcNow.AddDays(1);

        return Ok(new AuthResponseDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.FamilyId,
            family?.Name,
            token,
            expiresAt));
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    /// <returns>Current user's profile information</returns>
    /// <response code="200">Returns user information</response>
    /// <response code="401">User is not authenticated or token is invalid</response>
    /// <remarks>
    /// Requires a valid JWT token in the Authorization header.
    /// Returns the currently authenticated user's profile including:
    /// - User ID and email
    /// - First and last name
    /// - Role (Parent or Child)
    /// - Family ID and name (if assigned to a family)
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUser()
    {
        var user = await _accountService.GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        var family = user.FamilyId.HasValue
            ? await _context.Families.FindAsync(user.FamilyId.Value)
            : null;

        return Ok(new UserInfoDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.FamilyId,
            family?.Name));
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    /// <param name="dto">Current and new password</param>
    /// <returns>Success or failure</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Password change failed (e.g., incorrect current password)</response>
    /// <response code="401">User is not authenticated</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var currentUser = await _accountService.GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        var result = await _accountService.ChangePasswordAsync(currentUser.Id, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "PASSWORD_CHANGE_FAILED",
                    message = string.Join(", ", result.Errors.Select(e => e.Description))
                }
            });
        }

        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    /// <param name="dto">Email address to send reset link</param>
    /// <returns>Success message (regardless of whether email exists for security)</returns>
    /// <response code="200">Reset email sent if account exists</response>
    /// <remarks>
    /// For security, this endpoint always returns success even if the email doesn't exist.
    /// If the email is registered, a password reset link will be sent.
    /// </remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _accountService.ForgotPasswordAsync(dto.Email);

        // Always return success for security (don't reveal if email exists)
        return Ok(new { message = "If your email is registered, you will receive a password reset link shortly" });
    }

    /// <summary>
    /// Reset password using reset token from email
    /// </summary>
    /// <param name="dto">Email, reset token, and new password</param>
    /// <returns>Success or failure</returns>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Reset failed (e.g., invalid or expired token)</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await _accountService.ResetPasswordAsync(dto.Email, dto.ResetToken, dto.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "PASSWORD_RESET_FAILED",
                    message = string.Join(", ", result.Errors.Select(e => e.Description))
                }
            });
        }

        return Ok(new { message = "Password reset successfully" });
    }
}
