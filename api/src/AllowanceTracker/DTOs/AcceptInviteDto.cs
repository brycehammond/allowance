using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Request to accept an invite and set password (for new users)
/// </summary>
public record AcceptInviteDto(
    [Required]
    string Token,

    [Required]
    [EmailAddress]
    string Email,

    [Required]
    [StringLength(100, MinimumLength = 6)]
    string Password);
