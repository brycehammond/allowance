using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Api.V1;

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
    [HttpPost("register/parent")]
    [AllowAnonymous]
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
    [HttpPost("register/child")]
    [Authorize(Roles = "Parent")]
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
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
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
            .FirstAsync(u => u.Email == dto.Email);

        var token = _jwtService.GenerateToken(user);
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
    /// Get current authenticated user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
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
}
