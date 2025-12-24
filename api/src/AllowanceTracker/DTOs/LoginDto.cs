using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Request model for user authentication
/// </summary>
/// <param name="Email">User's email address</param>
/// <param name="Password">User's password (minimum 6 characters with at least one digit)</param>
/// <param name="RememberMe">Whether to persist the authentication session</param>
public record LoginDto(
    [Required][EmailAddress] string Email,
    [Required] string Password,
    bool RememberMe = false);
