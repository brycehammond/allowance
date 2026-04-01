using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Request model for external (social) authentication via Google or Apple
/// </summary>
/// <param name="Provider">Authentication provider ("Google" or "Apple")</param>
/// <param name="IdToken">ID token from the authentication provider</param>
/// <param name="FamilyName">Family name (required for new user registration)</param>
/// <param name="FirstName">User's first name (Apple only sends this on first authorization)</param>
/// <param name="LastName">User's last name (Apple only sends this on first authorization)</param>
public record ExternalLoginDto(
    [Required] string Provider,
    [Required] string IdToken,
    string? FamilyName = null,
    string? FirstName = null,
    string? LastName = null);
